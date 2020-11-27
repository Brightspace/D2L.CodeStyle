﻿using System;
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

		/// <remarks>
		/// Check that a type declaration (class, struct, record) always
		/// produces immutable values.
		/// </remarks>
		public bool CheckDeclaration( INamedTypeSymbol type ) {
			var result = true;

			var members = type.GetMembers()
				// Exclude static members.
				// Immutability is a property of values, [Immutable] (etc.) are
				// judgements about the immutability of values of some type.
				// Static fields/properties are global variables scoped to
				// particular types, but are not a factor in the immutability
				// of the values (instances) of that type.
				.Where( m => !m.IsStatic );

			if( type.TypeKind == TypeKind.Class ) {
				// Check that the base class is immutable for classes
				var baseClassOk = m_context.IsImmutable(
					type.BaseType,
					ImmutableTypeKind.Instance,
					() => GetLocationOfBaseClass( type ),
					out var diag
				);

				if( !baseClassOk ) {
					m_diagnosticSink( diag );
					result = false;
					// We can keep looking for more errors after this.
				}
			}

			foreach( var member in members ) {
				if( !CheckMember( member ) ) {
					result = false;
				}
			}

			return result;
		}

		/// <remarks>
		/// Check that a member (e.g. field or property) always produces immutable
		/// values.
		/// </remarks>
		public bool CheckMember( ISymbol member ) {
			if ( MutabilityAuditor.IsAudited( member, out var location ) ) {
				// If they have one of the auditing attributes, run the
				// checks anyway and error if they are unnecessary
				if( CheckMember( diagnosticSink: _ => { }, member ) ) {
					m_diagnosticSink(
						Diagnostic.Create(
							Diagnostics.UnnecessaryMutabilityAnnotation,
							location
						)
					);
				}

				// Audit annotations means this counts as immutable always
				return true;
			}

			return CheckMember( m_diagnosticSink, member );
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
			if ( field.IsImplicitlyDeclared ) {
				// These correspond to auto-properties. That case gets handled
				// in CheckProperty instead.
				return true;
			}

			var decl = field.DeclaringSyntaxReferences.Single()
				.GetSyntax() as VariableDeclaratorSyntax;

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
				// Indexer properties are just glorified method syntax and
				// don't hold state.
				// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/
				return true;
			}

			if ( prop.IsImplicitlyDeclared ) {
				// records have implicitly declared properties like
				// EqualityContract which are OK but don't have a
				// PropertyDeclarationSyntax etc.
				return true;
			}

			var propInfo = GetPropertyStuff(
				prop,
				prop.DeclaringSyntaxReferences.Single().GetSyntax()
			);

			if ( !propInfo.IsAutoImplemented ) {
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
				isReadOnly: propInfo.IsReadOnly,
				typeSyntax: propInfo.TypeSyntax,
				nameSyntax: propInfo.Identifier,
				initializer: propInfo.Initializer
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

			var stuff = GetStuffToCheckForMember(
				type,
				typeSyntax,
				initializer
			);

			if ( stuff == null ) {
				// null is a signal that there is nothing further that needs to
				// be checked
				return immutable;
			}

			var (typeToCheck, checkKind, getLocation) = stuff.Value;

			if( !m_context.IsImmutable( typeToCheck, checkKind, getLocation, out var diagnostic ) ) {
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
		private (ITypeSymbol, ImmutableTypeKind, Func<Location>)? GetStuffToCheckForMember(
			ITypeSymbol memberType,
			TypeSyntax memberTypeSyntax,
			ExpressionSyntax initializer
		) {
			if( initializer == null ) {
				return (
					memberType,
					ImmutableTypeKind.Total,
					() => memberTypeSyntax.GetLocation()
				);
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

			if ( initializer is BaseObjectCreationExpressionSyntax _ ) {
				// When we have a new T() we don't need to worry about the value
				// being anything other than an instance of T.
				return (
					typeToCheck,
					ImmutableTypeKind.Instance,
					() => initializer.GetLocation()
				);
			}

			// In general we need to handle subtypes.
			return (
				typeToCheck,
				ImmutableTypeKind.Total,
				() => initializer.GetLocation()
			);
		}

		private static Location GetLocationOfMember( ISymbol s ) =>s
			.DeclaringSyntaxReferences.Single()
			.GetSyntax()
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
				.Cast<TypeDeclarationSyntax>()
				.Where( r => r.BaseList != null )
				// Take _at most_ the first item from each BaseList.Types
				.SelectMany( r => r.BaseList.Types.Take( 1 ) );

			// Find the first candidate that is a class type.
			foreach( var candidate in candidates ) {
				var model = m_compilation.GetSemanticModel(
					candidate.SyntaxTree
				);

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

		/// <summary>
		/// Return info about a property, wether its the classic kind or one
		/// from a concise record declaration.
		/// </summary>
		private (
			TypeSyntax TypeSyntax,
			SyntaxToken Identifier,
			ExpressionSyntax Initializer,
			bool IsAutoImplemented,
			bool IsReadOnly
		) GetPropertyStuff( IPropertySymbol symbol, SyntaxNode syntax ) {
			bool isInitOnly = symbol.SetMethod != null && symbol.SetMethod.IsInitOnly;

			// init-only is as good as readonly for the rest of the analyzer.
			bool isReadOnly = isInitOnly || symbol.IsReadOnly;

			return syntax switch {
				PropertyDeclarationSyntax prop => (
					prop.Type,
					prop.Identifier,

					// Only return the initializer syntax for readonly
					// properties. init-only properties can be overwritten by
					// callers (rather than just constructors) which are well
					// outside the scope of our analysis.
					isInitOnly ? null : prop.Initializer?.Value,

					prop.IsAutoImplemented(),
					isReadOnly
				),
				ParameterSyntax param => (
					param.Type,
					param.Identifier,

					// We never care about initializers for these types of
					// properties because they are always init-only and can
					// be overwritten by any caller.
					Initializer: null,

					IsAutoImplemented: true,
					isReadOnly
				),
				_ => throw new NotImplementedException()
			};
		}
	}
}
