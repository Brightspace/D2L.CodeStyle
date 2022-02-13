using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Immutability;
public sealed partial class ImmutabilityAnalyzer {

	private static class ImmutableTypeParameterArgumentAnalysis {

		public static void Register(
			CompilationStartAnalysisContext context,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext
		) {
			// Type arguments of method return types
			// Type arguments of method parameters
			context.RegisterSymbolAction(
				ctx => {
					var symbol = (IMethodSymbol)ctx.Symbol;

					// Ignore auto-implemened property methods
					switch( symbol.MethodKind ) {
						case MethodKind.PropertyGet:
						case MethodKind.PropertySet:
							return;
					}

					SyntaxNode getBaseSyntax() => symbol.DeclaringSyntaxReferences[ 0 ].GetSyntax( ctx.CancellationToken );

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						symbol.ReturnType,
						() => {
							SyntaxNode syntax = getBaseSyntax();
							return getBaseSyntax() switch {
								MethodDeclarationSyntax methodDeclaration => methodDeclaration.ReturnType,
								_ => syntax,
							};
						}
					);

					for( int i = 0; i < symbol.Parameters.Length; ++i ) {
						IParameterSymbol parameter = symbol.Parameters[ i ];

						AnalyzeTypeRecursive(
							ctx.ReportDiagnostic,
							annotationsContext,
							immutabilityContext,
							parameter.Type,
							() => {
								SyntaxNode syntax = getBaseSyntax();

								if( syntax is not MethodDeclarationSyntax methodDeclaration ) {
									return syntax;
								}

								return methodDeclaration
									.ParameterList
									.Parameters[ i ]
									.Type;
							}
						);
					}
				},
				SymbolKind.Method
			);

			// Type arguments of local function return types
			// Type arguments of local function parameters
			context.RegisterSyntaxNodeAction(
				ctx => {
					var syntax = (LocalFunctionStatementSyntax)ctx.Node;

					ISymbol? maybeSymbol = ctx.SemanticModel.GetDeclaredSymbol(
						syntax,
						ctx.CancellationToken
					);

					if( maybeSymbol is not IMethodSymbol symbol ) {
						return;
					}

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						symbol.ReturnType,
						() => syntax.ReturnType
					);

					for( int i = 0; i < symbol.Parameters.Length; ++i ) {
						IParameterSymbol parameter = symbol.Parameters[ i ];

						AnalyzeTypeRecursive(
							ctx.ReportDiagnostic,
							annotationsContext,
							immutabilityContext,
							parameter.Type,
							() => {
								return syntax
									.ParameterList
									.Parameters[ i ]
									.Type;
							}
						);
					}
				},
				SyntaxKind.LocalFunctionStatement
			);

			// Type arguments of method invocations
			// Type arguments of containing types for statically invoked methods
			context.RegisterOperationAction(
				ctx => {
					var operation = (IInvocationOperation)ctx.Operation;

					SyntaxNodeOrToken getSyntax() => operation.Syntax switch {
						InvocationExpressionSyntax invocation => invocation.Expression,
						_ => operation.Syntax
					};

					AnalyzeMethodUsage(
						ctx,
						annotationsContext,
						immutabilityContext,
						operation.TargetMethod,
						getSyntax
					);
				},
				OperationKind.Invocation
			);

			// Type arguments of method references
			// Type arguments of containing types for statically referenced methods
			context.RegisterOperationAction(
				ctx => {
					var operation = (IMethodReferenceOperation)ctx.Operation;

					AnalyzeMethodUsage(
						ctx,
						annotationsContext,
						immutabilityContext,
						operation.Method,
						() => operation.Syntax
					);
				},
				OperationKind.MethodReference
			);

			// Type arguments of field types
			context.RegisterSymbolAction(
				ctx => {
					var symbol = (IFieldSymbol)ctx.Symbol;

					SyntaxNodeOrToken getSyntax() {
						SyntaxNode syntaxNode = symbol.DeclaringSyntaxReferences[ 0 ].GetSyntax( ctx.CancellationToken );

						return syntaxNode switch {
							FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Declaration.Type,
							VariableDeclaratorSyntax variableDeclarator => ( (VariableDeclarationSyntax)variableDeclarator.Parent! ).Type,
							_ => syntaxNode,
						};
					}

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						symbol.Type,
						getSyntax
					);
				},
				SymbolKind.Field
			);

			// Type arguments of containing types of statically referenced fields
			context.RegisterOperationAction(
				ctx => {
					var operation = (IFieldReferenceOperation)ctx.Operation;

					AnalyzeMaybeStaticMemberAccess(
						ctx,
						annotationsContext,
						immutabilityContext,
						operation.Field,
						() => operation.Syntax
					);
				},
				OperationKind.FieldReference
			);

			// Type arguments of property types
			context.RegisterSymbolAction(
				ctx => {
					var symbol = (IPropertySymbol)ctx.Symbol;

					if( symbol.Type is not INamedTypeSymbol type ) {
						return;
					}

					SyntaxNodeOrToken getSyntax() {
						SyntaxNode syntaxNode = symbol.DeclaringSyntaxReferences[ 0 ].GetSyntax( ctx.CancellationToken );

						return syntaxNode switch {
							IndexerDeclarationSyntax indexer => indexer.Type,
							ParameterSyntax parameter => ( parameter.Type ?? (SyntaxNode)parameter ),
							PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Type,
							_ => syntaxNode,
						};
					}

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						type,
						getSyntax
					);
				},
				SymbolKind.Property
			);

			// Type arguments of containing types of referenced properties for static properties
			context.RegisterOperationAction(
				ctx => {
					var operation = (IPropertyReferenceOperation)ctx.Operation;

					AnalyzeMaybeStaticMemberAccess(
						ctx,
						annotationsContext,
						immutabilityContext,
						operation.Property,
						() => operation.Syntax
					);
				},
				OperationKind.PropertyReference
			);

			// Type arguments of created objects
			context.RegisterOperationAction(
				ctx => {
					var operation = (IObjectCreationOperation)ctx.Operation;

					SyntaxNodeOrToken getSyntax() => operation.Syntax switch {
						ImplicitObjectCreationExpressionSyntax implicitObjectCreation => implicitObjectCreation.NewKeyword,
						ObjectCreationExpressionSyntax objectCreation => objectCreation.Type,
						_ => operation.Syntax
					};

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						operation.Type!,
						getSyntax
					);
				},
				OperationKind.ObjectCreation
			);

			// Type arguments of defined variables
			context.RegisterOperationAction(
				ctx => {
					var operation = (IVariableDeclarationOperation)ctx.Operation;

					SyntaxNodeOrToken getSyntax() => operation.Syntax switch {
						VariableDeclarationSyntax variableDeclaration => variableDeclaration.Type,
						_ => operation.Syntax
					};

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						operation.GetDeclaredVariables()[ 0 ].Type,
						getSyntax
					);
				},
				OperationKind.VariableDeclaration
			);

			// Type arguments of defined variables
			context.RegisterOperationAction(
				ctx => {
					SyntaxNodeOrToken syntax = ctx.Operation.Syntax;

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						ctx.Operation.Type!,
						() => syntax
					);
				},
				OperationKind.DeclarationExpression,
				OperationKind.Discard
			);

			// Type arguments on implemented types
			context.RegisterSymbolAction(
				ctx => {
					var symbol = (INamedTypeSymbol)ctx.Symbol;

					foreach( INamedTypeSymbol @interface in symbol.Interfaces ) {
						AnalyzeTypeRecursive(
							ctx.ReportDiagnostic,
							annotationsContext,
							immutabilityContext,
							@interface,
							() => getSyntax( symbol, @interface, ctx.CancellationToken )
						);
					}

					if( symbol.BaseType != null ) {
						AnalyzeTypeRecursive(
							ctx.ReportDiagnostic,
							annotationsContext,
							immutabilityContext,
							symbol.BaseType,
							() => getSyntax( symbol, symbol.BaseType, ctx.CancellationToken )
						);
					}

					SyntaxNodeOrToken getSyntax(
						INamedTypeSymbol typeSymbol,
						INamedTypeSymbol baseTypeSymbol,
						CancellationToken cancellationToken
					) {
						SyntaxNodeOrToken? anySyntax = null;
						foreach( var reference in typeSymbol.DeclaringSyntaxReferences ) {
							var syntax = (TypeDeclarationSyntax)reference.GetSyntax( cancellationToken );
							anySyntax = syntax.Identifier;

							var baseTypes = syntax.BaseList?.Types;
							if( baseTypes == null ) {
								continue;
							}

							SemanticModel model = ctx.Compilation.GetSemanticModel( syntax.SyntaxTree );
							foreach( var baseTypeSyntax in baseTypes ) {
								TypeSyntax typeSyntax = baseTypeSyntax.Type;

								ITypeSymbol? thisTypeSymbol = model.GetTypeInfo( typeSyntax, cancellationToken ).Type;

								if( baseTypeSymbol.Equals( thisTypeSymbol, SymbolEqualityComparer.Default ) ) {
									return baseTypeSyntax.Type;
								}
							}
						}

						if( !anySyntax.HasValue ) {
							throw new InvalidOperationException();
						}

						return anySyntax.Value;
					}
				},
				SymbolKind.NamedType
			);

			// type arguments of types in typeof expressions
			context.RegisterOperationAction(
				ctx => {
					var operation = (ITypeOfOperation)ctx.Operation;

					var syntax = ( (TypeOfExpressionSyntax)operation.Syntax ).Type;

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						operation.TypeOperand,
						() => syntax
					);
				},
				OperationKind.TypeOf
			);

			// type arguments of types in "foo is T" expressions
			context.RegisterOperationAction(
				ctx => {
					var operation = (IIsTypeOperation)ctx.Operation;

					var syntax = (BinaryExpressionSyntax)operation.Syntax;

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						operation.TypeOperand,
						() => syntax.Right
					);
				},
				OperationKind.IsType
			);

			// type arguments of types in:
			//   "foo is not T"
			//   case T:        (switch statement)
			//   T =>           (switch expression)
			context.RegisterOperationAction(
				ctx => {
					var operation = (ITypePatternOperation)ctx.Operation;

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						operation.NarrowedType,
						() => ( (TypePatternSyntax)operation.Syntax ).Type
					);
				},
				OperationKind.TypePattern
			);

			// type arguments of types in:
			//   "foo is not T x"
			//   cast T x:        (switch statement)
			//   T x =>           (switch expression)
			context.RegisterOperationAction(
				ctx => {
					var operation = (IDeclarationPatternOperation)ctx.Operation;

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						operation.NarrowedType,
						() => ( (DeclarationPatternSyntax)operation.Syntax ).Type
					);
				},
				OperationKind.DeclarationPattern
			);

			// type arguments of types in casts:
			//   (T)foo
			//   foo as T
			context.RegisterOperationAction(
				ctx => {
					var operation = (IConversionOperation)ctx.Operation;

					if( operation.IsImplicit ) {
						return;
					}

					SyntaxNodeOrToken getSyntax() => operation.Syntax switch {
						BinaryExpressionSyntax binaryExpression => binaryExpression.Right,
						CastExpressionSyntax castExpression => castExpression.Type,
						_ => operation.Syntax
					};

					AnalyzeTypeRecursive(
						ctx.ReportDiagnostic,
						annotationsContext,
						immutabilityContext,
						operation.Type!,
						getSyntax
					);
				},
				OperationKind.Conversion
			);
		}

		private static void AnalyzeMethodUsage(
			OperationAnalysisContext ctx,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext,
			IMethodSymbol method,
			Func<SyntaxNodeOrToken> getAnalyzedSyntax
		) {
			AnalyzeTypeArgumentsRecursive(
				ctx.ReportDiagnostic,
				annotationsContext,
				immutabilityContext,
				method.TypeArguments,
				method.TypeParameters,
				getAnalyzedSyntax
			);

			AnalyzeMaybeStaticMemberAccess(
				ctx,
				annotationsContext,
				immutabilityContext,
				method,
				getAnalyzedSyntax
			);
		}

		private static void AnalyzeMaybeStaticMemberAccess(
			OperationAnalysisContext ctx,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext,
			ISymbol member,
			Func<SyntaxNodeOrToken> getAnalyzedSyntax
		) {
			if( !member.IsStatic ) {
				return;
			}

			AnalyzeTypeRecursive(
				ctx.ReportDiagnostic,
				annotationsContext,
				immutabilityContext,
				member.ContainingType,
				SelectLeftSyntax( getAnalyzedSyntax )
			);
		}

		private static void AnalyzeTypeRecursive(
			Action<Diagnostic> reportDiagnostic,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext,
			ITypeSymbol type,
			Func<SyntaxNodeOrToken> getAnalyzedSyntax
		) {
			INamedTypeSymbol? namedType = GetNamedTypeRecursive( type );
			if( namedType is null ) {
				return;
			}

			if( namedType.ContainingType != null ) {
				AnalyzeTypeRecursive(
					reportDiagnostic,
					annotationsContext,
					immutabilityContext,
					namedType.ContainingType,
					SelectLeftSyntax( getAnalyzedSyntax )
				);
			}

			AnalyzeTypeArgumentsRecursive(
				reportDiagnostic,
				annotationsContext,
				immutabilityContext,
				namedType.TypeArguments,
				namedType.TypeParameters,
				getAnalyzedSyntax
			);

			static INamedTypeSymbol? GetNamedTypeRecursive( ITypeSymbol type ) => type switch {
				INamedTypeSymbol namedType => namedType,
				IArrayTypeSymbol arrayType => GetNamedTypeRecursive( arrayType.ElementType ),
				_ => null
			};
		}

		private static Func<SyntaxNodeOrToken> SelectLeftSyntax( Func<SyntaxNodeOrToken> getSyntax ) =>
			() => SelectLeftSyntax( getSyntax() );

		private static SyntaxNodeOrToken SelectLeftSyntax( SyntaxNodeOrToken syntax ) {
			if( syntax.IsToken ) {
				return syntax;
			}

			return syntax.AsNode() switch {
				MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
				QualifiedNameSyntax qualifiedName => qualifiedName.Left,
				_ => syntax,
			};
		}

		private static void AnalyzeTypeArgumentsRecursive(
			Action<Diagnostic> reportDiagnostic,
			AnnotationsContext annotationsContext,
			ImmutabilityContext immutabilityContext,
			ImmutableArray<ITypeSymbol> typeArguments,
			ImmutableArray<ITypeParameterSymbol> typeParameters,
			Func<SyntaxNodeOrToken> getAnalyzedSyntax
		) {
			getAnalyzedSyntax = SelectRightSyntax( getAnalyzedSyntax );

			for( var i = 0; i < typeArguments.Length; ++i ) {
				ITypeSymbol argument = typeArguments[ i ];
				ITypeParameterSymbol typeParameter = typeParameters[ i ];

				SyntaxNodeOrToken getThisArgument() => GetTypeArgumentSyntax( getAnalyzedSyntax(), i );

				AnalyzeTypeRecursive(
					reportDiagnostic,
					annotationsContext,
					immutabilityContext,
					argument,
					getThisArgument
				);

				if( !annotationsContext.Objects.Immutable.IsDefined( typeParameter ) ) {
					continue;
				}

				if( !immutabilityContext.IsImmutable(
					new ImmutabilityQuery(
						ImmutableTypeKind.Total,
						argument
					),
					getLocation: () => getThisArgument().GetLocation(),
					out Diagnostic diagnostic
				) ) {
					// TODO: not necessarily a good diagnostic for this use-case
					reportDiagnostic( diagnostic );
				}
			}
		}

		private static Func<SyntaxNodeOrToken> SelectRightSyntax( Func<SyntaxNodeOrToken> getSyntax )
			=> () => SelectRightSyntax( getSyntax() );

		private static SyntaxNodeOrToken SelectRightSyntax( SyntaxNodeOrToken syntax ) {
			if( syntax.IsToken ) {
				return syntax;
			}

			return syntax.AsNode() switch {
				ArrayTypeSyntax arrayType => arrayType.ElementType,
				DeclarationExpressionSyntax declarationExpression =>
					declarationExpression.Designation is ParenthesizedVariableDesignationSyntax parenthesizedDesignation
						? parenthesizedDesignation
						: declarationExpression.Type,
				MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
				QualifiedNameSyntax qualifiedName => qualifiedName.Right,
				_ => syntax,
			};
		}

		private static SyntaxNodeOrToken GetTypeArgumentSyntax( SyntaxNodeOrToken syntax, int n ) {
			if( syntax.IsToken ) {
				return syntax;
			}

			return syntax.AsNode() switch {
				GenericNameSyntax genericName => genericName.TypeArgumentList.Arguments[ n ],
				ParenthesizedVariableDesignationSyntax parenthesizedVariableDesignation => parenthesizedVariableDesignation.Variables[ n ],
				TupleTypeSyntax tupleType => tupleType.Elements[ n ].Type,
				_ => syntax,
			};
		}

	}
}
