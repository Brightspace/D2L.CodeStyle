using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Pinning {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class MustBeDeserializableAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.MustBeDeserializableRequiresRecursivelyPinned,
			Diagnostics.MustBePinnedRequiresPinned,
			Diagnostics.ArgumentShouldBeMustBePinned,
			Diagnostics.ArgumentShouldBeDeserializable,
			Diagnostics.PinningAttributesShouldBeInTheInterfaceIfInImplementations
			);

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private void OnCompilationStart(
			CompilationStartAnalysisContext context
		) {
			INamedTypeSymbol? plain = context.Compilation.GetTypeByMetadataName(PinnedAnalyzerHelper.MustBePinnedAttributeName );
			INamedTypeSymbol? recursive = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.MustBeDeserializableAttributeName );

			if( recursive != null
				&& plain != null ) {
				ImmutableList<MustBePinnedType>? mustBePinnedTypes = ImmutableList.Create(
					new MustBePinnedType( plain, false, Diagnostics.MustBePinnedRequiresPinned, Diagnostics.ArgumentShouldBeMustBePinned, recursive ),
					new MustBePinnedType( recursive, true, Diagnostics.MustBeDeserializableRequiresRecursivelyPinned, Diagnostics.ArgumentShouldBeDeserializable, null )
				);
				var additionalFiles = context.Options.AdditionalFiles;
				context.RegisterOperationAction( ( ctx ) =>
					AnalyzeInvocation( ctx, mustBePinnedTypes, additionalFiles ),
					OperationKind.Invocation );

				context.RegisterOperationAction( ( ctx ) => {
					AnalyzeObjectCreation(ctx, mustBePinnedTypes, additionalFiles);
				}, OperationKind.ObjectCreation );

				context.RegisterOperationAction( ( ctx ) => {
					AnalyzeSetters( ctx, mustBePinnedTypes, additionalFiles );
				}, OperationKind.PropertyReference );

				context.RegisterOperationAction( ( ctx ) => {
					AnalyzeFieldSetting( ctx, mustBePinnedTypes, additionalFiles );
				}, OperationKind.FieldReference );

				context.RegisterSymbolAction( ( ctx ) => {
					AnalyzeMethodDeclaration( ctx, mustBePinnedTypes, additionalFiles );
				}, SymbolKind.Method );
			}
		}

		private void AnalyzeMethodDeclaration(
			SymbolAnalysisContext context,
			ImmutableList<MustBePinnedType> mustBePinnedTypes,
			ImmutableArray<AdditionalText> additionalFiles ) {
			var symbol = context.Symbol as IMethodSymbol;
			if( symbol == null ) {
				return;
			}

			var interfaceMethod = symbol.ExplicitInterfaceImplementations.FirstOrDefault()
			    ?? symbol.GetImplementedMethods().FirstOrDefault();
			if( interfaceMethod == null ) {
				return;
			}

			// check generic type arguments
			for( int i = 0; i < symbol.TypeArguments.Length; i++ ) {
				foreach( var pinnedSymbol in mustBePinnedTypes ) {
					if( PinnedAnalyzerHelper.TryGetPinnedAttribute( symbol.TypeArguments[i], pinnedSymbol.PinnedAttributeSymbol, out _ ) ) {
						if( !PinnedAnalyzerHelper.TryGetPinnedAttribute( interfaceMethod.TypeArguments[i], pinnedSymbol.PinnedAttributeSymbol, out _ ) ) {
							context.ReportDiagnostic( Diagnostic.Create( Diagnostics.PinningAttributesShouldBeInTheInterfaceIfInImplementations, symbol.TypeArguments[i].Locations.FirstOrDefault() ));
						}
					}
				}
			}
			// check method parameters
			for( int i = 0; i < symbol.Parameters.Length; i++ ) {
				foreach( var pinnedSymbol in mustBePinnedTypes ) {
					if( PinnedAnalyzerHelper.TryGetPinnedAttribute( symbol.Parameters[i], pinnedSymbol.PinnedAttributeSymbol, out _ ) ) {
						if( !PinnedAnalyzerHelper.TryGetPinnedAttribute( interfaceMethod.Parameters[i], pinnedSymbol.PinnedAttributeSymbol, out _ ) ) {
							context.ReportDiagnostic( Diagnostic.Create( Diagnostics.PinningAttributesShouldBeInTheInterfaceIfInImplementations, symbol.Parameters[i].Locations.FirstOrDefault() ));
						}
					}
				}
			}
		}

		private void AnalyzeFieldSetting(
			OperationAnalysisContext context,
			ImmutableList<MustBePinnedType> mustBePinnedTypes,
			ImmutableArray<AdditionalText> additionalFiles ) {
			var operation = context.Operation as IFieldReferenceOperation;
			if( operation == null || !( operation.Parent is IAssignmentOperation assignmentOperation ) ) {
				return;
			}

			var syntax = assignmentOperation.Syntax;
			var model = assignmentOperation.SemanticModel;
			var type = assignmentOperation.Value.Type;


			var definitionSymbol = model?.GetSymbolInfo( assignmentOperation.Value.Syntax ).Symbol;
			if( definitionSymbol == null) {
				return;
			}
			var baseType = definitionSymbol.ContainingType;
			if(baseType == null) {
				return;
			}

			var parentGenericType = baseType.TypeArguments.FirstOrDefault( t => t.Equals( type, SymbolEqualityComparer.Default ) );
			if( parentGenericType == null ) {
				return;
			}

			foreach(var pinningType in mustBePinnedTypes) {
				if(!PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute(parentGenericType, pinningType, out _)) {
					continue;
				}

				// find the argument to the type being set and check it against the pinning type
				if( !PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( definitionSymbol, pinningType, out _ ) ) {
					context.ReportDiagnostic( Diagnostic.Create( pinningType.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
				}
			}
		}

		private void AnalyzeSetters(
			OperationAnalysisContext context,
			ImmutableList<MustBePinnedType> mustBePinnedTypes,
			ImmutableArray<AdditionalText> additionalFiles ) {
			var operation = context.Operation as IPropertyReferenceOperation;
			if( operation == null || !( operation.Parent is IAssignmentOperation assignmentOperation ) ) {
				return;
			}
			var type = operation.Property.OriginalDefinition.Type;

			if( type.TypeKind != TypeKind.TypeParameter ) {
				return;
			}

			INamedTypeSymbol? pinnedAttributeSymbol = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.PinnedAttributeName );
			if( pinnedAttributeSymbol == null ) {
				return;
			}

			var isExemptFromPinning = ( ITypeSymbol symbol ) => {
				var inAllowedList = PinnedAnalyzerHelper.AllowedUnpinnedTypes( additionalFiles, context.Compilation );
				return PinnedAnalyzerHelper.IsExemptFromPinning( symbol, inAllowedList, out _ );
			};

			var syntax = operation.Syntax;
			var model = operation.SemanticModel;

			if( model == null ) {
				return;
			}

			foreach( var pinningType in mustBePinnedTypes ) {
				if( PinnedAnalyzerHelper.TryGetPinnedAttribute( type, pinningType.PinnedAttributeSymbol, out _ ) ) {

					var definitionSymbol = model.GetSymbolInfo( assignmentOperation.Value.Syntax ).Symbol;
					var argSymbol = assignmentOperation.Value.Type;
					if(argSymbol == null || definitionSymbol == null) {
						return;
					}

					if(definitionSymbol is IParameterSymbol parameterSymbol ) {
						argSymbol = parameterSymbol.Type;
					}

					var parentMethod = syntax.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
					IEnumerable<ITypeSymbol> genericMethodArguments = Enumerable.Empty<ITypeSymbol>();
					IEnumerable<ITypeSymbol> genericBaseTypeArguments = Enumerable.Empty<ITypeSymbol>();
					IMethodSymbol? parentMethodSymbol = parentMethod != null ? model.GetDeclaredSymbol( parentMethod ) : null;

					if( parentMethodSymbol != null ) {
						genericMethodArguments = genericMethodArguments.Concat( parentMethodSymbol.TypeArguments );
						if( parentMethodSymbol.ContainingType != null ) {
							genericBaseTypeArguments = parentMethodSymbol.ContainingType.TypeArguments;
						}
					}

					// we want to enforce pinning here if the parameter is a type parameter and missing must be pinned on the method even if it's on the class
					if( argSymbol.TypeKind == TypeKind.TypeParameter
						) {
						if( !PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( definitionSymbol, pinningType, out _ ) ) {
							context.ReportDiagnostic( Diagnostic.Create( pinningType.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
						}
						return;
					}

					ValidatePinning(
						context: context,
						pinLocation: syntax.GetLocation(),
						pinnedAttributeSymbol: pinnedAttributeSymbol,
						pinningType: pinningType,
						argSymbol: argSymbol,
						definitionSymbol: definitionSymbol,
						isExemptFromPinning: isExemptFromPinning,
						genericArguments: genericMethodArguments,
						genericBaseArguments: genericBaseTypeArguments );
				}
			}
		}

		private void AnalyzeObjectCreation(
			OperationAnalysisContext context,
			ImmutableList<MustBePinnedType> mustBePinnedTypes,
			ImmutableArray<AdditionalText> additionalFiles ) {

			var operation = context.Operation as IObjectCreationOperation;
			if(operation == null) {
				return;
			}
			INamedTypeSymbol? pinnedAttributeSymbol = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.PinnedAttributeName );
			if( pinnedAttributeSymbol == null ) {
				return;
			}

			var isExemptFromPinning = ( ITypeSymbol symbol ) => {
				var inAllowedList = PinnedAnalyzerHelper.AllowedUnpinnedTypes( additionalFiles, context.Compilation );
				return PinnedAnalyzerHelper.IsExemptFromPinning( symbol, inAllowedList, out _ );
			};
			var typeSymbol = operation.Type as INamedTypeSymbol;
			if( operation.Constructor != null) {
				var originalTypeArgs = operation.Constructor.ContainingType.OriginalDefinition.TypeArguments;
				foreach(var pinningType in mustBePinnedTypes) {
					for( int i = 0; i < originalTypeArgs.Length; i++ ) {
						var current = originalTypeArgs[i];
						if( PinnedAnalyzerHelper.TryGetPinnedAttribute( current, pinningType.PinnedAttributeSymbol, out _ ) ) {
							var argSymbol = operation.Constructor.ContainingType.TypeArguments[ i ];
							if(!PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute(argSymbol, pinningType, out _)) {
								var syntax = operation.Syntax as ObjectCreationExpressionSyntax;
								var model = operation.SemanticModel;

								if( syntax == null || model == null ) {
									continue;
								}

								var operationSymbol = model.GetSymbolInfo( syntax ).Symbol ;

								var parentMethod = syntax.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

								IEnumerable<ITypeSymbol> genericMethodArguments = Enumerable.Empty<ITypeSymbol>();
								IEnumerable<ITypeSymbol> genericBaseTypeArguments = Enumerable.Empty<ITypeSymbol>();
								IMethodSymbol? parentMethodSymbol = parentMethod != null ? model.GetDeclaredSymbol( parentMethod ) : null;

								if( parentMethodSymbol != null ) {
									genericMethodArguments = genericMethodArguments.Concat( parentMethodSymbol.TypeArguments );
									if( parentMethodSymbol.ContainingType != null ) {
										genericBaseTypeArguments = parentMethodSymbol.ContainingType.TypeArguments;
									}
								}

								ISymbol definitionSymbol = current;

								var matchingTypeArgument = operation.Arguments.SingleOrDefault( a => a.Type!= null && a.Type.Equals( current, SymbolEqualityComparer.Default ) );


								if( matchingTypeArgument != null ) {
									var matchingInvocationArgument = operation.Arguments[ operation.Arguments.IndexOf( matchingTypeArgument ) ];
									ISymbol? symbol = model.GetSymbolInfo( matchingInvocationArgument.Value.Syntax ).Symbol;
									if( symbol != null ) {
										definitionSymbol = symbol;
									}
								}

								ValidatePinning(
									context: context,
									pinLocation: syntax.GetLocation(),
									pinnedAttributeSymbol: pinnedAttributeSymbol,
									pinningType: pinningType,
									argSymbol: argSymbol,
									definitionSymbol: definitionSymbol,
									isExemptFromPinning: isExemptFromPinning,
									genericArguments: genericMethodArguments,
									genericBaseArguments: genericBaseTypeArguments );
							}
						}
					}
				}
			}


		}

		private void AnalyzeInvocation(
			OperationAnalysisContext context,
			ImmutableList<MustBePinnedType> mustBePinnedTypes,
			ImmutableArray<AdditionalText> additionalFiles ) {

			var operation = context.Operation as IInvocationOperation;

			if( operation == null || mustBePinnedTypes == null ) {
				return;
			}
			var invocation = operation.Syntax as InvocationExpressionSyntax;
			if( invocation == null ) {
				return;
			}

			INamedTypeSymbol? pinnedAttributeSymbol = context.Compilation.GetTypeByMetadataName( PinnedAnalyzerHelper.PinnedAttributeName );
			if( pinnedAttributeSymbol == null ) {
				return;
			}

			IMethodSymbol methodSymbol = operation.TargetMethod;
			SyntaxReference? match = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
			if( match == null ) {
				return;
			}

			SyntaxNode matchNode = match.GetSyntax();
			var methodDeclaration = matchNode as MethodDeclarationSyntax;

			if( methodDeclaration == null ) {
				return;
			}

			// collect generic arguments from the method and the base type for passing into future checks so MustBePinned can be associated with them
			var parentMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

			IEnumerable<ITypeSymbol> genericMethodArguments = Enumerable.Empty<ITypeSymbol>();
			IEnumerable<ITypeSymbol> genericBaseTypeArguments = Enumerable.Empty<ITypeSymbol>();
			SemanticModel model = operation.SemanticModel!;
			IMethodSymbol? parentMethodSymbol = parentMethod != null ? model.GetDeclaredSymbol( parentMethod ) : null;

			if( parentMethodSymbol != null ) {
				genericMethodArguments = genericMethodArguments.Concat( parentMethodSymbol.TypeArguments );
				if( parentMethodSymbol.ContainingType != null ) {
					genericBaseTypeArguments = parentMethodSymbol.ContainingType.TypeArguments;
				}
			}

			var isExemptFromPinning = ( ITypeSymbol symbol ) => {
				var inAllowedList = PinnedAnalyzerHelper.AllowedUnpinnedTypes( additionalFiles, context.Compilation );
				return PinnedAnalyzerHelper.IsExemptFromPinning( symbol, inAllowedList, out _ );
			};

			// check type arguments if they exist on the invoked method definition


			if( methodDeclaration.TypeParameterList != null ) {
				// check for generic arguments that must be pinned from the invoked method
				foreach( ITypeSymbol invokedMethodTypeArgument in methodSymbol.OriginalDefinition.TypeArguments ) {
					foreach( MustBePinnedType pinningType in mustBePinnedTypes ) {
						if( PinnedAnalyzerHelper.TryGetPinnedAttribute( invokedMethodTypeArgument, pinningType.PinnedAttributeSymbol, out _ ) ) {
							// find the matching type parameter from the invocation

							int argIndex = methodSymbol.OriginalDefinition.TypeArguments.IndexOf( invokedMethodTypeArgument );
							ImmutableArray<ITypeSymbol> arguments = methodSymbol.TypeArguments;
							ISymbol definitionSymbol = invokedMethodTypeArgument;

							ITypeSymbol invocationTypeArgument = arguments[argIndex];

							var matchingTypeArgument = methodSymbol.Parameters.SingleOrDefault( p => p.Type.Equals( invocationTypeArgument, SymbolEqualityComparer.Default ) );


							if(matchingTypeArgument != null) {
								var matchingInvocationArgument = operation.Arguments[ methodSymbol.Parameters.IndexOf( matchingTypeArgument ) ];
								ISymbol? symbol = model.GetSymbolInfo( matchingInvocationArgument.Value.Syntax ).Symbol;
								if( symbol != null ) {
									definitionSymbol = symbol;
								}
							}

							// check the type is pinned if concrete, or has the appropriate attribute if it comes from the method or base type
							ValidatePinning(
								context: context,
								pinLocation: invocation.GetLocation(),
								pinnedAttributeSymbol: pinnedAttributeSymbol,
								pinningType: pinningType,
								argSymbol: invocationTypeArgument!,
								definitionSymbol: definitionSymbol,
								isExemptFromPinning: isExemptFromPinning,
								genericArguments: genericMethodArguments,
								genericBaseArguments: genericBaseTypeArguments );
						}
					}
				}
			}

			// check parameters to the method comparing the invocation parameters to the called method
			foreach( IParameterSymbol argument in methodSymbol.OriginalDefinition.Parameters ) {
				SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
				foreach( MustBePinnedType pinningType in mustBePinnedTypes ) {
					if( PinnedAnalyzerHelper.TryGetPinnedAttribute( argument, pinningType.PinnedAttributeSymbol, out _ ) ) {
						// find the matching type parameter
						int argIndex = methodSymbol.OriginalDefinition.Parameters.IndexOf( argument );

						ArgumentSyntax? arg = invocation.ArgumentList.Arguments.FirstOrDefault( a => a.NameColon?.ToString() == argument.Name );

						if( arg == null ) {
							if( arguments.Count <= argIndex ) {
								continue;
							}

							arg = invocation.ArgumentList.Arguments[argIndex];
						}
						if( arg == null ) {
							continue;
						}
						var argSymbol = operation.Arguments.FirstOrDefault( a => argument.Equals( a.Parameter, SymbolEqualityComparer.Default ) );

						// fetch the variable definition
						// the following operations require handling a wide variety of cases to accomplish the same thing without the semantic model
						ISymbol? symbol = model.GetSymbolInfo( arg.Expression ).Symbol;
						TypeInfo typeInfo = model.GetTypeInfo( arg.Expression );

						if( symbol == null ) {
							// check for typeof(T)
							if( symbol == null && argSymbol?.Value is ITypeOfOperation typeOp ) {
								if( IsPinnedProperly( pinnedAttributeSymbol, pinningType, typeOp.TypeOperand ) ) {
									continue;
								}
								symbol = typeOp.TypeOperand;
								// check the result of casting or other non-obvious conversions
							} else if( typeInfo.Type != null
								&& IsPinnedProperly( pinnedAttributeSymbol, pinningType, typeInfo.Type ) ) {
								continue;
							}
						}

						if( symbol == null ) {
							context.ReportDiagnostic( Diagnostic.Create( pinningType.Descriptor, arg.GetLocation() ) );
							continue;
						}

						// if the appropriate MustBePinned attribute is on the definition of the variable in question then it's fine unless it has invalid type arguments
						if( PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( symbol, pinningType, out _ ) ) {
							INamedTypeSymbol? typeSymbol = symbol as INamedTypeSymbol;
							if( typeSymbol is not { IsGenericType: true } ) {
								continue;
							}
						}

						// try getting the type from common ways the variable might be assigned
						var parameterSymbol = symbol as IParameterSymbol;
						ITypeSymbol? type = typeInfo.Type ?? parameterSymbol?.Type;
						if( symbol is IFieldSymbol fieldSymbol ) {
							type = fieldSymbol.Type;
						} else if( symbol is IMethodSymbol methodParameterSymbol ) {
							type = methodParameterSymbol.ReceiverType ?? methodParameterSymbol.ReturnType;
						}

						if( type != null ) {
							// if there isn't an appropriate MustBePinnedAttribute on it then check the type and all it's generic arguments
							ValidatePinning(
								context: context,
								pinLocation: arg.GetLocation(),
								pinnedAttributeSymbol: pinnedAttributeSymbol,
								pinningType: pinningType,
								argSymbol: type,
								definitionSymbol: symbol,
								isExemptFromPinning: isExemptFromPinning,
								genericArguments: genericMethodArguments,
								genericBaseArguments: genericBaseTypeArguments );

						} else {
							context.ReportDiagnostic( Diagnostic.Create( pinningType.Descriptor, arg.GetLocation() ) );
						}
					}

				}
			}
		}

		private static bool ValidatePinning(
			OperationAnalysisContext context,
			Location pinLocation,
			INamedTypeSymbol pinnedAttributeSymbol,
			MustBePinnedType pinningType,
			ITypeSymbol argSymbol,
			ISymbol definitionSymbol,
			Func<ITypeSymbol, bool> isExemptFromPinning,
			IEnumerable<ITypeSymbol>? genericArguments = null,
			IEnumerable<ITypeSymbol>? genericBaseArguments = null
			) {
			Queue<ITypeSymbol> queue = new Queue<ITypeSymbol>();
			queue.Enqueue( argSymbol );
			// only report the dianostic once on the invocation
			bool unpinnedArgument = false;

			while( queue.Any() ) {
				ITypeSymbol current = queue.Dequeue();

				// if a generic type parameter check it has the appropriate attribute
				if( current.Kind == SymbolKind.TypeParameter ) {
					// check if defined in the parent method
					if( genericArguments != null ) {
						ITypeSymbol matchingArgument = genericArguments.FirstOrDefault( a => a.Equals( current, SymbolEqualityComparer.Default ) );
						if( matchingArgument != null
							&& !PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( current, pinningType, out _ ) ) {
							context.ReportDiagnostic( Diagnostic.Create( pinningType.ParameterShouldBeChangedDescriptor, matchingArgument.Locations.First() ) );
						}
					}
					// check if it comes from the base type
					if( genericBaseArguments != null ) {
						ITypeSymbol baseTypeArgument = genericBaseArguments.FirstOrDefault( a => a.Equals( current, SymbolEqualityComparer.Default ) );
						if( baseTypeArgument != null ) {
							if( !PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( current, pinningType, out _ ) ) {
								context.ReportDiagnostic( Diagnostic.Create( pinningType.ParameterShouldBeChangedDescriptor, baseTypeArgument.Locations.First() ) );
							}
							if( definitionSymbol is IParameterSymbol && !PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( definitionSymbol, pinningType, out _ ) ) {
								context.ReportDiagnostic( Diagnostic.Create( pinningType.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
							}
						}
					}
				// if there's a concrete type that isn't pinned then mark the location given
				} else if( !unpinnedArgument
					&& !isExemptFromPinning( current )
					&& !IsPinnedProperly( pinnedAttributeSymbol, pinningType, current ) ) {
					if( definitionSymbol is IParameterSymbol ) {
						if( !PinnedAnalyzerHelper.HasAppropriateMustBePinnedAttribute( definitionSymbol, pinningType, out _ ) ) {
							context.ReportDiagnostic( Diagnostic.Create( pinningType.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
						}
					} else {
						unpinnedArgument = true;
						context.ReportDiagnostic( Diagnostic.Create( pinningType.Descriptor, pinLocation ) );
					}
				}

				if( current is INamedTypeSymbol genericArgSymbol ) {
					foreach( ITypeSymbol arg in genericArgSymbol.TypeArguments ) {
						queue.Enqueue( arg );
					}
				}
			}

			return unpinnedArgument;
		}

		private static bool IsPinnedProperly( INamedTypeSymbol pinnedAttributeSymbol, MustBePinnedType pinningType, ITypeSymbol argSymbol ) {
			if( pinnedAttributeSymbol.TypeKind == TypeKind.TypeParameter ) {
				return true;
			}
			if( !PinnedAnalyzerHelper.TryGetPinnedAttribute( argSymbol, pinnedAttributeSymbol, out AttributeData? attribute ) ) {
				return false;
			}
			if( !pinningType.Recursive ) {
				return true;
			}
			bool isRecursive = (bool)attribute?.ConstructorArguments[2].Value!;
			return isRecursive;
		}
	}
}
