using System;
using System.Collections.Generic;
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
					new ImmutabilityQuery(
						ImmutableTypeKind.Instance,
						type.BaseType
					),
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
			if( MutabilityAuditor.CheckAudited(
				member,
				m_diagnosticSink,
				out var location
			) ) {
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

			// Get all possible assignments of the field
			var assignments = GetAssignments( field, decl.Initializer?.Value );

			return CheckFieldOrProperty(
				diagnosticSink,
				member: field,
				type: field.Type,
				isReadOnly: field.IsReadOnly,
				typeSyntax: type,
				nameSyntax: decl.Identifier,
				assignments
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

			// Get all possible assignments of the property
			var assignments = GetAssignments( prop, propInfo.Initializer );

			return CheckFieldOrProperty(
				diagnosticSink,
				member: prop,
				type: prop.Type,
				isReadOnly: propInfo.IsReadOnly,
				typeSyntax: propInfo.TypeSyntax,
				nameSyntax: propInfo.Identifier,
				assignments
			);
		}

		private bool CheckFieldOrProperty(
			DiagnosticSink diagnosticSink,
			ISymbol member,
			ITypeSymbol type,
			bool isReadOnly,
			TypeSyntax typeSyntax,
			SyntaxToken nameSyntax,
			IEnumerable<ExpressionSyntax> assignments
		) {
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

				// Note: we're going to go looking for other errors.
				// There shouldn't be any "return true" below, it should
				// always be conditional on isReadOnly.
			}

			// If our field or property is a type that is always immutable then
			// we can stop looking (all that matters is that we are readonly).
			if( m_context.IsImmutable(
				new ImmutabilityQuery(
					ImmutableTypeKind.Total,
					type
				),
				typeSyntax.GetLocation,
				out var typeDiagnostic
			) ) {
				return isReadOnly;
			}

			// Our field or property could hold mutable values, but it's safe
			// as long as we always assign immutable values to it.

			// Usually that would be difficult, but if we're readonly its a lot
			// easier: we only have to look in our constructors (this applies
			// even to protected readonly; these can't be set in derived
			// constructors.)

			// TODO: we don't really need this, but it has been our current
			// behaviour... if we have a readonly field with no writes that's
			// worth a diagnostic on its own (there are built in ones for that)
			// but technically it doesn't add mutability.
			// If we remove this we can rename typeDiagnostic to _ again.
			if ( !assignments.Any() ) {
				diagnosticSink( typeDiagnostic );
				return false;
			}

			var allAssignmentsAreOfImmutableValues = true;

			foreach( var assignment in assignments ) {
				var query = GetStuffToCheckForAssignment( assignment );

				if( query == null ) {
					// null is a signal that there is nothing further that needs to
					// be checked for this assignment.
					continue;
				}

				if( m_context.IsImmutable(
					query.Value,
					() => assignment.GetLocation(),
					out var diagnostic
				) ) {
					continue;
				}

				diagnosticSink( diagnostic );

				// We're going to keep looking at all writes to surface as many
				// relevant diagnostics as possible.
				allAssignmentsAreOfImmutableValues = false;
			}

			return isReadOnly && allAssignmentsAreOfImmutableValues;
		}


		/// <summary>
		/// A readonly field or property may be re-assigned in constructors.
		/// This method finds all assignments of the field/property.
		/// </summary>
		/// <param name="memberSymbol">The field/property to search for</param>
		/// <param name="initializer">The initialization with the declaration</param>
		/// <returns>An immutable list of assignments</returns>
		private IEnumerable<ExpressionSyntax> GetAssignments(
			ISymbol memberSymbol,
			ExpressionSyntax initializer
		) {
			// Retrieve a list of assignment expressions from any constructors
			// within the class
			var assignmentExpressions = ( memberSymbol.ContainingSymbol as INamedTypeSymbol )!
				.Constructors
				.Where( constructorSymbol => !constructorSymbol.IsImplicitlyDeclared )
				.Where( constructorSymbol => constructorSymbol.IsStatic == memberSymbol.IsStatic )
				.Select( constructorSymbol => constructorSymbol.DeclaringSyntaxReferences.Single() )
				.Select( sr => sr.GetSyntax() )
				.SelectMany( constructorSyntax => constructorSyntax.DescendantNodes() )
				.OfType<AssignmentExpressionSyntax>()
				.Where( assignmentSyntax => IsAnAssignmentTo( assignmentSyntax, memberSymbol ) )
				.Select( assignmentSyntax => assignmentSyntax.Right );

			if ( initializer != null ) {
				assignmentExpressions = assignmentExpressions
					.Append( initializer );
			}

			return assignmentExpressions;
		}

		private bool IsAnAssignmentTo(
			AssignmentExpressionSyntax assignmentSyntax,
			ISymbol memberSymbol
		) {
			var semanticModel = m_compilation.GetSemanticModel(
				memberSymbol.DeclaringSyntaxReferences.Single().GetSyntax().SyntaxTree
			);

			var leftSideSymbol = semanticModel.GetSymbolInfo( assignmentSyntax.Left )
				.Symbol;

			return SymbolEqualityComparer.Default.Equals(
				memberSymbol,
				leftSideSymbol
			);
		}

		/// <summary>
		/// For a field/property assignment, figure out what needs to be checked for it.
		/// </summary>
		/// <param name="assignment">The assignment syntax for the field/property (possibly null)</param>
		/// <returns>null if no checks are needed, otherwise the query we need to run.</returns>
		private ImmutabilityQuery? GetStuffToCheckForAssignment(
			ExpressionSyntax assignment
		) {
			// When we have an assignment we use it to narrow our check, e.g.
			//
			//   private readonly object m_lock = new object();
			//
			// is safe even though object (the type of the field) in general is not.

			// Easy cases
			switch( assignment.Kind() ) {
				// Sometimes people explicitly (but needlessly) assign null in
				// an initializer... this is safe.
				case SyntaxKind.NullLiteralExpression:

				// Lambda initializers for readonly members are safe
				// because they can only close over other members, which
				// will be checked independently, or static members of
				// another class, which are also analyzed
				//
				// TODO: only true (in general) for initializers; non-static
				// ones in constructors can close over mutable stack vars!
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
					return null;
			}

			var model = m_compilation.GetSemanticModel( assignment.SyntaxTree );
			var typeInfo = model.GetTypeInfo( assignment );

			// Type can be null in the case of an implicit conversion where the
			// expression alone doesn't have a type. For example:
			//   int[] foo = { 1, 2, 3 };
			var typeToCheck = typeInfo.Type ?? typeInfo.ConvertedType;

			if( assignment is BaseObjectCreationExpressionSyntax _ ) {
				// When we have a new T() we don't need to worry about the value
				// being anything other than an instance of T.
				return new ImmutabilityQuery(
					ImmutableTypeKind.Instance,
					typeToCheck
				);
			}

			// In general we need to handle subtypes.
			return new ImmutabilityQuery(
				ImmutableTypeKind.Total,
				typeToCheck
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
