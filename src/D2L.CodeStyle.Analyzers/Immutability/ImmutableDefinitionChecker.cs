using System;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class ImmutableDefinitionChecker {

		internal delegate void DiagnosticSink( Diagnostic diagnostic );

		private readonly Compilation m_compilation;
		private readonly DiagnosticSink m_diagnosticSink;
		private readonly ImmutabilityContext m_context;

		public ImmutableDefinitionChecker(
			Compilation compilation,
			DiagnosticSink diagnosticSink,
			ImmutabilityContext context
		) {
			m_compilation = compilation;
			m_diagnosticSink = diagnosticSink;
			m_context = context;
		}

		public bool CheckDeclaration( INamedTypeSymbol type ) {
			var result = true;

			var members = type.GetMembers()
				// Exclude static members.
				// Immutability is, for us, a property held by values. A type
				// can be immutable in one of two ways and these are statements
				// about values of that type.
				// Static members are not tied to values/instances by
				// definition. Although we care about them otherwise, TODO explain
				.Where( m => !m.IsStatic );

			// Check that the base class is immutable for classes
			if( type.TypeKind == TypeKind.Class ) {
				var baseClassOk = m_context.IsImmutable(
					type.BaseType,
					ImmutableTypeKind.Instance,
					GetLocationOfBaseClass( type ),
					out var diag
				);

				if( !baseClassOk ) {
					m_diagnosticSink( diag );
					result = false;
					// We can keep looking for more errors after this.
				}
			}

			foreach( var member in members ) {
				if( IsAudited( member ) ) {
					if( CheckMember( diagnosticSink: _ => { }, member ) ) {
						m_diagnosticSink(
							Diagnostic.Create(
								Diagnostics.UnnecessaryMutabilityAnnotation,
								GetLocationOfAnnotation( member )
							)
						);
					}

					continue;
				}

				if( !CheckMember( m_diagnosticSink, member ) ) {
					result = false;
				}
			}

			return result;
		}

		private bool CheckMember( DiagnosticSink diagnosticSink, ISymbol member ) {
			switch( member.Kind ) {
				case SymbolKind.Field:
					return CheckField( diagnosticSink, member as IFieldSymbol );
				case SymbolKind.Property:
					return CheckProperty( diagnosticSink, member as IPropertySymbol );

				// These member types never contribute to mutability:
				case SymbolKind.Method:
				case SymbolKind.NamedType:
					return true;

				case SymbolKind.Event:
					diagnosticSink(
						Diagnostic.Create(
							Diagnostics.EventMemberMutable,
							GetLocationOfMember( member )
						)
					);

					return false;

				// By default raise an alarm (in case we missed something, or
				// if there are new unsupported language features.)
				default:
					m_diagnosticSink(
						Diagnostic.Create(
							Diagnostics.UnexpectedMemberKind,
							GetLocationOfMember( member ),
							member.Name,
							member.Kind
						)
					);

					return false;
			}
		}

		private bool CheckField( DiagnosticSink diagnosticSink, IFieldSymbol field ) {
			// These correspond to auto-properties. That case gets handled in
			// CheckProperty instead.
			if ( field.IsImplicitlyDeclared ) {
				return true;
			}

			var decl = field.GetDeclarationSyntax<VariableDeclaratorSyntax>();
			var type = decl.FirstAncestorOrSelf<VariableDeclarationSyntax>().Type;

			return CheckFieldOrProperty(
				diagnosticSink,
				member: field,
				type: field.Type,
				isReadOnly: field.IsReadOnly,
				typeSyntax: type,
				nameSyntax: decl.Identifier,
				initializer: decl.Initializer?.Value
			);
		}


		private bool CheckProperty( DiagnosticSink diagnosticSink, IPropertySymbol prop ) {
			if ( prop.IsIndexer ) {
				// Indexer properties are just glorified method syntax and don't hold state.
				// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/
				return true;
			}

			var decl = prop.GetDeclarationSyntax<PropertyDeclarationSyntax>();

			if ( !decl.IsAutoImplemented() ) {
				// Properties that are auto-implemented have an implicit
				// backing field that may be mutable. Otherwise, properties are
				// just sugar for getter/setter methods and don't themselves
				// contribute to mutability.
				return true;
			}

			return CheckFieldOrProperty(
				diagnosticSink,
				member: prop,
				type: prop.Type,
				isReadOnly: prop.IsReadOnly,
				typeSyntax: decl.Type,
				nameSyntax: decl.Identifier,
				initializer: decl.Initializer?.Value
			);
		}

		private bool CheckFieldOrProperty(
			DiagnosticSink diagnosticSink,
			ISymbol member,
			ITypeSymbol type,
			bool isReadOnly,
			TypeSyntax typeSyntax,
			SyntaxToken nameSyntax,
			ExpressionSyntax initializer
		) {
			var immutable = true;

			if ( !isReadOnly ) {
				diagnosticSink(
					Diagnostic.Create(
						Diagnostics.MemberIsNotReadOnly,
						nameSyntax.GetLocation(),
						member.Kind,
						member.Name,
						member.ContainingType.Name
					)
				);

				immutable = false;
			}

			var stuff = GetStuffToCheckForMember( type, typeSyntax, initializer );

			// null is a signal that there is nothing fruther that needs to be checked
			if ( stuff == null ) {
				return immutable;
			}

			var (typeToCheck, checkKind, diagnosticLocation) = stuff.Value;

			if( !m_context.IsImmutable( typeToCheck, checkKind, diagnosticLocation, out var diagnostic ) ) {
				diagnosticSink( diagnostic );
				immutable = false;
			}

			return immutable;
		}

		/// <summary>
		/// For a field/property, figure out:
		/// * which type to check,
		/// * what kind of check to do, and
		/// * where to put any diagnostics.
		/// </summary>
		/// <param name="memberType">The type of the field/property</param>
		/// <param name="memberTypeSyntax">The syntax for the type of the field/property</param>
		/// <param name="initializer">The initializer syntax for the field/property (possibly null)</param>
		/// <returns>null if no checks are needed, otherwise a bunch of stuff.</returns>
		private (ITypeSymbol, ImmutableTypeKind, Location)? GetStuffToCheckForMember(
			ITypeSymbol memberType,
			TypeSyntax memberTypeSyntax,
			ExpressionSyntax initializer
		) {
			if( initializer == null ) {
				return (memberType, ImmutableTypeKind.Total, memberTypeSyntax.GetLocation());
			}

			// When we have an initializer we use it to narrow our check, e.g.
			//
			//   private readonly object m_lock = new object();
			//
			// is safe even though object in general is not.
			//
			// TODO: there is a bug: https://github.com/Brightspace/D2L.CodeStyle/issues/319
			// The bug is that we need to check for other writes inside of our
			// constructors before narrowing too much!

			// Easy cases
			switch( initializer.Kind() ) {
				// This is perhaps a bit suspicious, because fields and
				// properties have to be readonly, but it is safe...
				case SyntaxKind.NullLiteralExpression:

				// Lambda initializers for readonly members are safe
				// because they can only close over other members, which
				// will be checked independently, or static members of
				// another class, which are also analyzed
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
					return null;
			}

			var model = m_compilation.GetSemanticModel( initializer.SyntaxTree );
			var typeInfo = model.GetTypeInfo( initializer );

			// Type can be null in the case of an implicit conversion where the
			// expression alone doesn't have a type. For example:
			//   int[] foo = { 1, 2, 3 };
			var typeToCheck = typeInfo.Type ?? typeInfo.ConvertedType;

			if ( initializer is ObjectCreationExpressionSyntax objCreation ) {
				// When we have a new T() we don't need to worry about the value
				// being anything other than an instance of T.
				return (typeToCheck, ImmutableTypeKind.Instance, objCreation.Type.GetLocation());
			}

			// In general we need to handle subtypes.
			return (typeToCheck, ImmutableTypeKind.Total, initializer.GetLocation());
		}

		private static bool IsAudited( ISymbol symbol ) {
			if( Attributes.Mutability.Audited.IsDefined( symbol ) ) {
				return true;
			}

			if( Attributes.Mutability.Unaudited.IsDefined( symbol ) ) {
				return true;
			}

			return false;
		}

		private static Location GetLocationOfAnnotation( ISymbol s ) {
			var audited = Attributes.Mutability.Audited.GetAll( s ).FirstOrDefault();
			if( audited != null ) {
				return audited.ApplicationSyntaxReference.GetSyntax().GetLocation();
			}

			var unaudited = Attributes.Mutability.Unaudited.GetAll( s ).FirstOrDefault();
			if( unaudited != null ) {
				return unaudited.ApplicationSyntaxReference.GetSyntax().GetLocation();
			}

			throw new InvalidOperationException( $"{nameof( GetLocationOfAnnotation )} should only be called on an annotated symbol" );
		}

		private static Location GetLocationOfMember( ISymbol s ) =>s
			.GetDeclarationSyntax<SyntaxNode>()
			.FirstAncestorOrSelf<MemberDeclarationSyntax>()
			.GetLocation();

		private Location GetLocationOfBaseClass( ITypeSymbol type ) {
			// Consider the following valid code:
			//
			// class Base {}
			// interface IA { }
			// interface IB { }
			//
			// partial class Foo { }
			// partial class Foo : Base { }
			// partial class Foo : Base, IA { }
			// partial class Foo : IB { }
			//
			// Our goal is to get one of the mentions of Base after "Foo :" to
			// place the diagnostic. There may be multiple, but for valid code
			// it will be the first type in the list for one of the decls.
			// (interfaces always go after the base class, if it is listed.)

			var candidates = type.DeclaringSyntaxReferences
				.Select( r => r.GetSyntax() )
				.Cast<ClassDeclarationSyntax>()
				.Where( r => r.BaseList != null )
				// Take _at most_ the first item from each BaseList.Types
				.SelectMany( r => r.BaseList.Types.Take( 1 ) );

			// Find the first candidate that is a class type.
			foreach( var candidate in candidates ) {
				var model = m_compilation.GetSemanticModel( candidate.SyntaxTree );
				var candidateInfo = model.GetTypeInfo( candidate.Type );

				if ( candidateInfo.Type == null ) {
					continue;
				}

				if ( candidateInfo.Type.TypeKind == TypeKind.Class ) {
					return candidate.GetLocation();
				}
			}

			// If we couldn't find a candidate just use the first class decl
			// as the diagnostic target. I'm not sure this can happen.
			return type.DeclaringSyntaxReferences.First()
				.GetSyntax()
				.GetLocation();
		}
	}
}
