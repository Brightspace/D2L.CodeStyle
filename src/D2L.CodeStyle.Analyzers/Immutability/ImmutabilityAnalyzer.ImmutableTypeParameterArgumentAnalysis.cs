using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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
			void AnalyzeMethod(
				OperationAnalysisContext ctx,
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

				AnalyzeMemberAccess(
					ctx,
					method,
					getAnalyzedSyntax
				);
			}

			void AnalyzeMemberAccess(
				OperationAnalysisContext ctx,
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

			// Type Argument on Methods
			context.RegisterOperationAction(
				ctx => {
					var operation = (IInvocationOperation)ctx.Operation;

					SyntaxNodeOrToken getSyntax() => operation.Syntax switch {
						InvocationExpressionSyntax invocation => invocation.Expression,
						_ => operation.Syntax
					};

					AnalyzeMethod(
						ctx,
						operation.TargetMethod,
						getSyntax
					);
				},
				OperationKind.Invocation
			);
			context.RegisterOperationAction(
				ctx => {
					var operation = (IMethodReferenceOperation)ctx.Operation;

					AnalyzeMethod(
						ctx,
						operation.Method,
						() => operation.Syntax
					);
				},
				OperationKind.MethodReference
			);
			context.RegisterOperationAction(
				ctx => {
					var operation = (IPropertyReferenceOperation)ctx.Operation;

					AnalyzeMemberAccess(
						ctx,
						operation.Property,
						() => operation.Syntax
					);
				},
				OperationKind.PropertyReference
			);
			context.RegisterOperationAction(
				ctx => {
					var operation = (IFieldReferenceOperation)ctx.Operation;

					AnalyzeMemberAccess(
						ctx,
						operation.Field,
						() => operation.Syntax
					);
				},
				OperationKind.FieldReference
			);

			// Type Arguments when creating objects
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

			// Type arguments when defining variables
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

			// Type arguments of field types
			context.RegisterSymbolAction(
				ctx => {
					var symbol = (IFieldSymbol)ctx.Symbol;

					if( symbol.Type is not INamedTypeSymbol type ) {
						return;
					}

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
						type,
						getSyntax
					);
				},
				SymbolKind.Field
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
							_ => throw new Exception( $"{syntaxNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {syntaxNode.ToFullString()}" ),
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
			getAnalyzedSyntax = SelectRightSyntaxRecursive( getAnalyzedSyntax );

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

				// TODO: this should eventually use information from ImmutableTypeInfo
				// however the current information about immutable type parameters
				// includes [Immutable] filling for what will instead be the upcoming
				// [OnlyIf] (e.g. it would be broken for IEnumerable<>)
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

		private static Func<SyntaxNodeOrToken> SelectRightSyntaxRecursive( Func<SyntaxNodeOrToken> getSyntax )
			=> () => SelectRightSyntaxRecursive( getSyntax() );

		private static SyntaxNodeOrToken SelectRightSyntaxRecursive( SyntaxNodeOrToken syntax ) {
			if( syntax.IsToken ) {
				return syntax;
			}

			return syntax.AsNode() switch {
				ArrayTypeSyntax arrayType => arrayType.ElementType,
				MemberAccessExpressionSyntax memberAccess => SelectRightSyntaxRecursive( memberAccess.Name ),
				QualifiedNameSyntax qualifiedName => SelectRightSyntaxRecursive( qualifiedName.Right ),
				_ => syntax,
			};
		}

		private static SyntaxNodeOrToken GetTypeArgumentSyntax( SyntaxNodeOrToken syntax, int n ) {
			if( syntax.IsToken ) {
				return syntax;
			}

			return syntax.AsNode() switch {
				GenericNameSyntax genericName => genericName.TypeArgumentList.Arguments[ n ],
				_ => syntax,
			};
		}

	}
}
