using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		private readonly AnnotationsContext m_annotationsContext;

		public ImmutableDefinitionChecker(
			Compilation compilation,
			DiagnosticSink diagnosticSink,
			ImmutabilityContext context,
			AnnotationsContext annotationsContext
		) {
			m_compilation = compilation;
			m_diagnosticSink = diagnosticSink;
			m_context = context;
			m_annotationsContext = annotationsContext;
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
				m_annotationsContext,
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
			IEnumerable<AssignmentInfo> assignments
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
				var kind = GetQueryForAssignment( assignment, out var query, out var diagnostic );

				switch( kind ) {
					case AssignmentQueryKind.NothingToCheck:
						continue;

					case AssignmentQueryKind.Hopeless:
						diagnosticSink( diagnostic );
						allAssignmentsAreOfImmutableValues = false;
						continue;

					case AssignmentQueryKind.ImmutabilityQuery:
						if( m_context.IsImmutable(
							query,
							() => assignment.Expression.GetLocation(),
							out diagnostic
						) ) {
							continue;
						}

						diagnosticSink( diagnostic );

						// We're going to keep looking at all writes to surface as many
						// relevant diagnostics as possible.
						allAssignmentsAreOfImmutableValues = false;
						continue;
				}
			}

			return isReadOnly && allAssignmentsAreOfImmutableValues;
		}

		private readonly struct AssignmentInfo {
			private AssignmentInfo(
				bool isInitializer,
				ExpressionSyntax expression,
				ITypeSymbol assignedType
			) {
				IsInitializer = isInitializer;
				Expression = expression;
				AssignedType = assignedType;
			}

			public bool IsInitializer { get; }
			public ExpressionSyntax Expression { get; }
			public ITypeSymbol AssignedType { get; }

			public static AssignmentInfo Create(
				SemanticModel model,
				bool isInitializer,
				ExpressionSyntax expression
			) {
				TypeInfo typeInfo = model.GetTypeInfo( expression );

				// Type can be null in the case of an implicit conversion where the
				// expression alone doesn't have a type. For example:
				//   int[] foo = { 1, 2, 3 };
				ITypeSymbol assignedType = typeInfo.Type ?? typeInfo.ConvertedType;

				return new AssignmentInfo(
					isInitializer: isInitializer,
					expression: expression,
					assignedType: assignedType
				);
			}

			public static AssignmentInfo Create(
				bool isInitializer,
				ExpressionSyntax expression,
				ITypeSymbol assignedType
			) {
				return new AssignmentInfo(
					isInitializer: isInitializer,
					expression: expression,
					assignedType: assignedType
				);
			}
		}


		/// <summary>
		/// A readonly field or property may be re-assigned in constructors.
		/// This method finds all assignments of the field/property.
		/// </summary>
		/// <param name="memberSymbol">The field/property to search for</param>
		/// <param name="initializer">The initialization with the declaration</param>
		/// <returns>An immutable list of assignments</returns>
		private IEnumerable<AssignmentInfo> GetAssignments(
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
				.Select( assignmentSyntax => GetAssignmentInfoIfToSymbolOrNull( assignmentSyntax, memberSymbol ) )
				.Where( info => info.HasValue )
				.Select( info => info.Value );

			if ( initializer != null ) {
				assignmentExpressions = assignmentExpressions.Append(
					AssignmentInfo.Create(
						model: m_compilation.GetSemanticModel( initializer.SyntaxTree ),
						isInitializer: true,
						expression: initializer
					)
				);
			}

			return assignmentExpressions;
		}

		private AssignmentInfo? GetAssignmentInfoIfToSymbolOrNull(
			AssignmentExpressionSyntax assignmentSyntax,
			ISymbol memberSymbol
		) {
			var semanticModel = m_compilation.GetSemanticModel( assignmentSyntax.SyntaxTree );

			var lhsExpressions = assignmentSyntax.Left switch {
				TupleExpressionSyntax tuple => tuple.Arguments.Select( arg => arg.Expression ).ToImmutableArray(),
				_ => ImmutableArray.Create( assignmentSyntax.Left )
			};

			for( var i = 0; i < lhsExpressions.Length; ++i ) {
				var lhs = lhsExpressions[ i ];

				var leftSideSymbol = semanticModel.GetSymbolInfo( lhs )
					.Symbol;

				if( !SymbolEqualityComparer.Default.Equals(
					memberSymbol,
					leftSideSymbol
				) ) {
					continue;
				}

				// LHS is just a single variable, no deconstruction happening
				if( lhsExpressions.Length == 1 ) {
					return AssignmentInfo.Create(
						model: semanticModel,
						isInitializer: false,
						expression: assignmentSyntax.Right
					);
				}

				// Value tuples are a common RHS assignment for deconstruction, but it's built-in and there's
				// no deconstruction method. Handle value tuples specifically if it is one.
				if( semanticModel.GetTypeInfo( assignmentSyntax.Right ).Type is INamedTypeSymbol assignedType
					&& assignedType.IsTupleType
				) {
					if( assignedType.TypeArguments.Length != lhsExpressions.Length ) {
						return AssignmentInfo.Create(
							isInitializer: false,
							expression: assignmentSyntax.Right,
							assignedType: null
						);
					}

					// If this is a value tuple literal then we can narrow in to the exact expression
					// responsible for the tuple item. This allows for additional specificity prior to conversions
					// or things like "known immutable method returns" (such as Array.Empty())
					ExpressionSyntax rhs = assignmentSyntax.Right switch {
						TupleExpressionSyntax tuple => tuple.Arguments[ i ].Expression,
						_ => assignmentSyntax.Right
					};

					return AssignmentInfo.Create(
						isInitializer: false,
						expression: rhs,
						assignedType: assignedType.TypeArguments[ i ]
					);
				}

				// Other types can participate in deconstruction by specifying a deconstruction method
				// public void Deconstruct( out T1 a, out T2 b ... )
				// These methods must have a unique number of arguments. e.g. the following are ambiguous
				//   public void Deconstruct( out string a, out string b )
				//   public void Deconstruct( out int a, out int b )
				// The overall deconstruction can be "nested" if a type's deconstruction includes a type which
				// is itself deconstructed as well
				DeconstructionInfo deconstruction = semanticModel.GetDeconstructionInfo( assignmentSyntax );

				// If there was no method, then:
				//   * This might've been something like a value tuple, where the deconstruction is implicit
				//   * This isn't a valid assignment (and thus should be compiler error)
				if( deconstruction.Method == null ) {
					return AssignmentInfo.Create(
						isInitializer: false,
						expression: assignmentSyntax.Right,
						assignedType: null
					);
				}

				// DeconstructionInfo is a tree. Each non-terminal node has a method which produces the values
				// Some child nodes may themselves have methods as well, producing more values (referred to as
				// "nested" deconstruction).
				//
				// Only handling the simple case of non-nested deconstruction, meaning the tree has a depth of 2
				// with a single method. In this case there should be a parameter for every variable being assigned to
				if( deconstruction.Method.Parameters.Length != lhsExpressions.Length ) {
					return AssignmentInfo.Create(
						isInitializer: false,
						expression: assignmentSyntax.Right,
						assignedType: null
					);
				}

				return AssignmentInfo.Create(
					isInitializer: false,
					expression: assignmentSyntax.Right,
					assignedType: deconstruction.Method.Parameters[ i ].Type
				);
			}

			return null;
		}

		private enum AssignmentQueryKind {
			NothingToCheck,
			ImmutabilityQuery,
			Hopeless
		}

		/// <summary>
		/// For a field/property assignment, figure out what needs to be checked for it.
		/// </summary>
		/// <param name="assignment">The assignment syntax for the field/property (possibly null)</param>
		/// <param name="query">The query to do (if the return value is AssignmentQueryKind.ImmutabilityQuery.)</param>
		/// <param name="diagnostic">A diagnostic to report (if the return value is AssignmentQueryKind.Hopeless.)</param>
		/// <returns>The details about what to check for this assignment</returns>
		private AssignmentQueryKind GetQueryForAssignment(
			AssignmentInfo assignment,
			out ImmutabilityQuery query,
			out Diagnostic diagnostic
		) {
			// When we have an assignment we use it to narrow our check, e.g.
			//
			//   private readonly object m_lock = new object();
			//
			// is safe even though object (the type of the field) in general is not.

			query = default;
			diagnostic = null;

			// Easy cases
			switch( assignment.Expression.Kind() ) {
				case SyntaxKind.NullLiteralExpression:
					// Sometimes people explicitly (but needlessly) assign null in
					// an initializer... this is safe.
					return AssignmentQueryKind.NothingToCheck;

				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
					if ( assignment.IsInitializer ) {
						// Lambda initializers for readonly members are safe
						// because they can only close over other members,
						// which will be checked independently, or static
						// members of another class, which are also analyzed.
						return AssignmentQueryKind.NothingToCheck;
					}

					// But for assignments inside a constructor we're in more
					// trouble. They could capture arbitrary state from their
					// lexical scope.

					// static functions can't capture state.
					if ( assignment.Expression.IsStaticFunction() ) {
						return AssignmentQueryKind.NothingToCheck;
					}

					// In general we must panic.
					diagnostic = Diagnostic.Create(
						Diagnostics.AnonymousFunctionsMayCaptureMutability,
						assignment.Expression.GetLocation()
					);

					return AssignmentQueryKind.Hopeless;

				case SyntaxKind.InvocationExpression:

					// Some methods are known to have return values that are
					// immutable (such as Enumerable.Empty()).
					// These should be considered immutable by the Analyzer.
					SemanticModel semanticModel = m_compilation.GetSemanticModel( assignment.Expression.SyntaxTree );
					if( semanticModel
						.GetSymbolInfo( assignment.Expression )
						.Symbol is not IMethodSymbol methodSymbol
					) {
						break;
					}

					if( m_context.IsReturnValueKnownToBeImmutable( methodSymbol ) ) {
						return AssignmentQueryKind.NothingToCheck;
					}

					break;
			}

			if( assignment.AssignedType == null
				|| assignment.AssignedType.TypeKind == TypeKind.Error
			) {
				diagnostic = Diagnostic.Create(
					Diagnostics.NonImmutableTypeHeldByImmutable,
					assignment.Expression.GetLocation(),
					"blarg", "blarg", "blarg"
				);
				return AssignmentQueryKind.Hopeless;
			}

			// If nothing above was caught, then fallback to querying.

			if( assignment.Expression is BaseObjectCreationExpressionSyntax _ ) {
				// When we have a new T() we don't need to worry about the value
				// being anything other than an instance of T.
				query = new ImmutabilityQuery(
					ImmutableTypeKind.Instance,
					type: assignment.AssignedType
				);
			} else {
				// In general we need to handle subtypes.
				query = new ImmutabilityQuery(
					ImmutableTypeKind.Total,
					type: assignment.AssignedType
				);
			}

			return AssignmentQueryKind.ImmutabilityQuery;
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
