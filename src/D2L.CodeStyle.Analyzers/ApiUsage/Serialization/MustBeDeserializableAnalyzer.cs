using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class MustBeDeserializableAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.MustBeDeserializableRequiresAppropriateAttribute,
			Diagnostics.ArgumentShouldBeDeserializable,
			Diagnostics.MustBeDeserializableAttributeShouldBeInTheInterfaceIfInImplementations );

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private void OnCompilationStart(
			CompilationStartAnalysisContext context
		) {
			DeserializableTypeInfo? deserializableTypeInfo = DeserializableAnalyzerHelper.GetDeserializableTypeInfo( context.Compilation );

			if( deserializableTypeInfo != null ) {
				ImmutableArray<AdditionalText> additionalFiles = context.Options.AdditionalFiles;
				context.RegisterOperationAction( ( ctx ) =>
					AnalyzeInvocation( ctx, deserializableTypeInfo, additionalFiles ),
					OperationKind.Invocation );

				context.RegisterOperationAction( ( ctx ) => {
					AnalyzeObjectCreation( ctx, deserializableTypeInfo, additionalFiles );
				}, OperationKind.ObjectCreation );

				context.RegisterOperationAction( ( ctx ) => {
					AnalyzeSetters( ctx, deserializableTypeInfo, additionalFiles );
				}, OperationKind.PropertyReference );

				context.RegisterOperationAction( ( ctx ) => {
					AnalyzeFieldSetting( ctx, deserializableTypeInfo, additionalFiles );
				}, OperationKind.FieldReference );

				context.RegisterSymbolAction( ( ctx ) => {
					AnalyzeMethodDeclaration( ctx, deserializableTypeInfo );
				}, SymbolKind.Method );
			}
		}

		private void AnalyzeMethodDeclaration(
			SymbolAnalysisContext context,
			DeserializableTypeInfo deserializableTypeInfo ) {
			IMethodSymbol? symbol = context.Symbol as IMethodSymbol;
			if( symbol == null ) {
				return;
			}

			IMethodSymbol? interfaceMethod = symbol.ExplicitInterfaceImplementations.FirstOrDefault()
			                                 ?? symbol.GetImplementedMethods().FirstOrDefault();
			if( interfaceMethod == null ) {
				return;
			}

			// check generic type arguments
			for( int i = 0; i < symbol.TypeArguments.Length; i++ ) {
				if( DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( symbol.TypeArguments[i], deserializableTypeInfo ) ) {
					if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( interfaceMethod.TypeArguments[i], deserializableTypeInfo ) ) {
						context.ReportDiagnostic( Diagnostic.Create( Diagnostics.MustBeDeserializableAttributeShouldBeInTheInterfaceIfInImplementations, symbol.TypeArguments[i].Locations.FirstOrDefault() ) );
					}
				}
			}

			// check method parameters
			for( int i = 0; i < symbol.Parameters.Length; i++ ) {
				if( DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( symbol.Parameters[i], deserializableTypeInfo ) ) {
					if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( interfaceMethod.Parameters[i], deserializableTypeInfo ) ) {
						context.ReportDiagnostic( Diagnostic.Create( Diagnostics.MustBeDeserializableAttributeShouldBeInTheInterfaceIfInImplementations, symbol.Parameters[i].Locations.FirstOrDefault() ) );
					}
				}
			}
		}

		private void AnalyzeFieldSetting(
			OperationAnalysisContext context,
			DeserializableTypeInfo deserializableTypeInfo,
			ImmutableArray<AdditionalText> additionalFiles ) {
			IFieldReferenceOperation? operation = context.Operation as IFieldReferenceOperation;
			if( operation == null || !( operation.Parent is IAssignmentOperation assignmentOperation ) ) {
				return;
			}

			SemanticModel? model = assignmentOperation.SemanticModel;
			ITypeSymbol? type = assignmentOperation.Value.Type;


			ISymbol? definitionSymbol = model?.GetSymbolInfo( assignmentOperation.Value.Syntax ).Symbol;
			if( definitionSymbol == null ) {
				return;
			}
			INamedTypeSymbol? baseType = definitionSymbol.ContainingType;
			if( baseType == null ) {
				return;
			}

			ITypeSymbol? parentGenericType = baseType.TypeArguments.FirstOrDefault( t => t.Equals( type, SymbolEqualityComparer.Default ) );
			if( parentGenericType == null ) {
				return;
			}

			if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( parentGenericType, deserializableTypeInfo ) ) {
				return;
			}

			// find the argument to the type being set and check it
			if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( definitionSymbol, deserializableTypeInfo ) ) {
				context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
			}
		}

		private void AnalyzeSetters(
			OperationAnalysisContext context,
			DeserializableTypeInfo deserializableTypeInfo,
			ImmutableArray<AdditionalText> additionalFiles ) {
			IPropertyReferenceOperation? operation = context.Operation as IPropertyReferenceOperation;
			if( operation == null || !( operation.Parent is IAssignmentOperation assignmentOperation ) ) {
				return;
			}
			ITypeSymbol type = operation.Property.OriginalDefinition.Type;

			if( type.TypeKind != TypeKind.TypeParameter ) {
				return;
			}

			Func<ITypeSymbol, bool> isExempt = DeserializableAnalyzerHelper.IsDeserializableWithoutSerializerAttribute( additionalFiles, context.Compilation );

			SyntaxNode syntax = operation.Syntax;
			SemanticModel? model = operation.SemanticModel;

			if( model == null ) {
				return;
			}

			if( DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( type, deserializableTypeInfo ) ) {

				ISymbol? definitionSymbol = model.GetSymbolInfo( assignmentOperation.Value.Syntax ).Symbol;
				ITypeSymbol? argSymbol = assignmentOperation.Value.Type;
				if( argSymbol == null || definitionSymbol == null ) {
					return;
				}

				if( definitionSymbol is IParameterSymbol parameterSymbol ) {
					argSymbol = parameterSymbol.Type;
				}

				MethodDeclarationSyntax? parentMethod = syntax.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
				IEnumerable<ITypeSymbol> genericMethodArguments = Enumerable.Empty<ITypeSymbol>();
				IEnumerable<ITypeSymbol> genericBaseTypeArguments = Enumerable.Empty<ITypeSymbol>();
				IMethodSymbol? parentMethodSymbol = parentMethod != null ? model.GetDeclaredSymbol( parentMethod ) : null;

				if( parentMethodSymbol != null ) {
					genericMethodArguments = genericMethodArguments.Concat( parentMethodSymbol.TypeArguments );
					if( parentMethodSymbol.ContainingType != null ) {
						genericBaseTypeArguments = parentMethodSymbol.ContainingType.TypeArguments;
					}
				}

				// we want to enforce checks here if the parameter is a type parameter and missing [MustBeDeserializable] on the method even if it's on the class
				if( argSymbol.TypeKind == TypeKind.TypeParameter
					) {
					if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( definitionSymbol, deserializableTypeInfo ) ) {
						context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
					}
					return;
				}

				ValidateIsDeserializable(
					context: context,
					location: syntax.GetLocation(),
					deserializableTypeInfo: deserializableTypeInfo,
					argSymbol: argSymbol,
					definitionSymbol: definitionSymbol,
					isExempt: isExempt,
					genericArguments: genericMethodArguments,
					genericBaseArguments: genericBaseTypeArguments );
			}

		}

		private void AnalyzeObjectCreation(
			OperationAnalysisContext context,
			DeserializableTypeInfo deserializableTypeInfo,
			ImmutableArray<AdditionalText> additionalFiles ) {

			IObjectCreationOperation? operation = context.Operation as IObjectCreationOperation;
			if( operation == null ) {
				return;
			}

			Func<ITypeSymbol, bool> isExempt = DeserializableAnalyzerHelper.IsDeserializableWithoutSerializerAttribute( additionalFiles, context.Compilation );
			INamedTypeSymbol? typeSymbol = operation.Type as INamedTypeSymbol;
			if( operation.Constructor == null ) {
				return;
			}

			// check type arguments
			ImmutableArray<ITypeSymbol> originalTypeArgs = operation.Constructor.ContainingType.OriginalDefinition.TypeArguments;
			ObjectCreationExpressionSyntax? syntax = operation.Syntax as ObjectCreationExpressionSyntax;
			SemanticModel? model = operation.SemanticModel;

			if( syntax == null || model == null ) {
				return;
			}

			MethodDeclarationSyntax? parentMethod = syntax.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

			if( parentMethod?.ToString()?.Contains( "INotificationQueueAppender.Enqueue<T>" ) ?? false ) {
				Console.WriteLine( "debug" );
			}

			IEnumerable<ITypeSymbol> genericMethodArguments = Enumerable.Empty<ITypeSymbol>();
			IEnumerable<ITypeSymbol> genericBaseTypeArguments = Enumerable.Empty<ITypeSymbol>();
			IMethodSymbol? parentMethodSymbol = parentMethod != null ? model.GetDeclaredSymbol( parentMethod ) : null;

			if( parentMethodSymbol != null ) {
				genericMethodArguments = genericMethodArguments.Concat( parentMethodSymbol.TypeArguments );
				if( parentMethodSymbol.ContainingType != null ) {
					genericBaseTypeArguments = parentMethodSymbol.ContainingType.TypeArguments;
				}
			}

			for( int i = 0; i < originalTypeArgs.Length; i++ ) {
				ITypeSymbol current = originalTypeArgs[i];
				if( DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( current, deserializableTypeInfo ) ) {
					ITypeSymbol argSymbol = operation.Constructor.ContainingType.TypeArguments[i];
					if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( argSymbol, deserializableTypeInfo ) ) {

						ISymbol? operationSymbol = model.GetSymbolInfo( syntax ).Symbol;
						ISymbol definitionSymbol = current;

						IArgumentOperation? matchingTypeArgument = operation.Arguments.SingleOrDefault( a => a.Type != null && a.Type.Equals( current, SymbolEqualityComparer.Default ) );


						if( matchingTypeArgument != null ) {
							IArgumentOperation matchingInvocationArgument = operation.Arguments[operation.Arguments.IndexOf( matchingTypeArgument )];
							ISymbol? symbol = model.GetSymbolInfo( matchingInvocationArgument.Value.Syntax ).Symbol;
							if( symbol != null ) {
								definitionSymbol = symbol;
							}
						}

						ValidateIsDeserializable(
							context: context,
							location: syntax.GetLocation(),
							deserializableTypeInfo: deserializableTypeInfo,
							argSymbol: argSymbol,
							definitionSymbol: definitionSymbol,
							isExempt: isExempt,
							genericArguments: genericMethodArguments,
							genericBaseArguments: genericBaseTypeArguments );
					}
				}

			}


			// check constructor parameters
			ImmutableArray<IParameterSymbol> constructorParameters = operation.Constructor.Parameters;
			if( syntax.ArgumentList == null ) {
				return;
			}

			for( int i = 0; i < constructorParameters.Length; i++ ) {
				IParameterSymbol current = constructorParameters[i];
				if( DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute(current, deserializableTypeInfo) ) {

					ArgumentSyntax? arg = syntax.ArgumentList.Arguments.FirstOrDefault( a => a.NameColon?.ToString() == current.Name );

					if( arg == null ) {
						if( syntax.ArgumentList.Arguments.Count <= i ) {
							continue;
						}

						arg = syntax.ArgumentList.Arguments[i];
					}

					IArgumentOperation? argSymbol = operation.Arguments.FirstOrDefault( a => current.Equals( a.Parameter, SymbolEqualityComparer.Default ) );

					// fetch the variable definition
					// the following operations require handling a wide variety of cases to accomplish the same thing without the semantic model
					ISymbol? symbol = model.GetSymbolInfo( arg.Expression ).Symbol;
					TypeInfo typeInfo = model.GetTypeInfo( arg.Expression );
					(bool deserializable, ITypeSymbol? type) = CheckIfDeserializableAndUpdateToUnderlyingType(
							context,
							ref symbol,
							argSymbol,
							arg.GetLocation(),
							typeInfo,
							deserializableTypeInfo
						);
					if( deserializable || symbol == null ) {
						continue;
					}

					if( type != null ) {
						// if there isn't an appropriate MustBeDeserializableAttribute on it then check the type and all it's generic arguments
						ValidateIsDeserializable(
							context: context,
							location: arg.GetLocation(),
							deserializableTypeInfo: deserializableTypeInfo,
							argSymbol: type,
							definitionSymbol: symbol,
							isExempt: isExempt,
							genericArguments: genericMethodArguments,
							genericBaseArguments: genericBaseTypeArguments );

					} else {
						context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.Descriptor, arg.GetLocation() ) );
					}
				}
			}
		}

		private void AnalyzeInvocation(
			OperationAnalysisContext context,
			DeserializableTypeInfo deserializableTypeInfo,
			ImmutableArray<AdditionalText> additionalFiles ) {

			IInvocationOperation? operation = context.Operation as IInvocationOperation;

			InvocationExpressionSyntax? invocation = operation?.Syntax as InvocationExpressionSyntax;
			if( invocation == null || operation == null ) {
				return;
			}

			IMethodSymbol methodSymbol = operation.TargetMethod;
			SyntaxReference? match = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();

			SyntaxNode? matchNode = match?.GetSyntax();

			if( matchNode is not MethodDeclarationSyntax methodDeclaration ) {
				return;
			}

			// collect generic arguments from the method and the base type for passing into future checks so MustBeDeserializable can be associated with them
			MethodDeclarationSyntax? parentMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

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

			Func<ITypeSymbol, bool> isExempt = DeserializableAnalyzerHelper.IsDeserializableWithoutSerializerAttribute( additionalFiles, context.Compilation );

			// check type arguments if they exist on the invoked method definition


			if( methodDeclaration.TypeParameterList != null ) {
				// check for generic arguments that must be deserializable from the invoked method
				foreach( ITypeSymbol invokedMethodTypeArgument in methodSymbol.OriginalDefinition.TypeArguments ) {
					if( DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute(invokedMethodTypeArgument, deserializableTypeInfo) ) {
						// find the matching type parameter from the invocation

						int argIndex = methodSymbol.OriginalDefinition.TypeArguments.IndexOf( invokedMethodTypeArgument );
						ImmutableArray<ITypeSymbol> arguments = methodSymbol.TypeArguments;
						ISymbol definitionSymbol = invokedMethodTypeArgument;

						ITypeSymbol invocationTypeArgument = arguments[argIndex];

						IParameterSymbol? matchingTypeArgument = methodSymbol.Parameters.FirstOrDefault( p => p.Type.Equals( invocationTypeArgument, SymbolEqualityComparer.Default ) );


						if( matchingTypeArgument != null ) {
							IArgumentOperation matchingInvocationArgument = operation.Arguments[methodSymbol.Parameters.IndexOf( matchingTypeArgument )];
							ISymbol? symbol = model.GetSymbolInfo( matchingInvocationArgument.Value.Syntax ).Symbol;
							if( symbol != null ) {
								definitionSymbol = symbol;
							}
						}

						// check the type is deserializable if concrete, or has the appropriate attribute if it comes from the method or base type
						ValidateIsDeserializable(
							context: context,
							location: invocation.GetLocation(),
							deserializableTypeInfo: deserializableTypeInfo,
							argSymbol: invocationTypeArgument!,
							definitionSymbol: definitionSymbol,
							isExempt: isExempt,
							genericArguments: genericMethodArguments,
							genericBaseArguments: genericBaseTypeArguments );
					}
				}
			}

			// check parameters to the method comparing the invocation parameters to the called method
			foreach( IParameterSymbol argument in methodSymbol.OriginalDefinition.Parameters ) {
				SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
				if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( argument, deserializableTypeInfo ) ) {
					continue;
				}

				// find the matching type parameter
				int argIndex = methodSymbol.OriginalDefinition.Parameters.IndexOf( argument );

				ArgumentSyntax? arg = invocation.ArgumentList.Arguments.FirstOrDefault( a => a.NameColon?.ToString() == argument.Name );

				if( arg == null ) {
					if( arguments.Count <= argIndex ) {
						continue;
					}

					arg = invocation.ArgumentList.Arguments[argIndex];
				}

				IArgumentOperation? argSymbol = operation.Arguments.FirstOrDefault( a => argument.Equals( a.Parameter, SymbolEqualityComparer.Default ) );

				// fetch the variable definition
				// the following operations require handling a wide variety of cases to accomplish the same thing without the semantic model
				ISymbol? symbol = model.GetSymbolInfo( arg.Expression ).Symbol;
				TypeInfo typeInfo = model.GetTypeInfo( arg.Expression );
				(bool isDeserializable, ITypeSymbol? type) = CheckIfDeserializableAndUpdateToUnderlyingType(
					context,
					ref symbol,
					argSymbol,
					arg.GetLocation(),
					typeInfo,
					deserializableTypeInfo
				);
				if( isDeserializable || symbol == null ) {
					continue;
				}

				if( type != null ) {
					// if there isn't the MustBeDeserializableAttribute on it then check the type and all it's generic arguments
					ValidateIsDeserializable(
						context: context,
						location: arg.GetLocation(),
						deserializableTypeInfo: deserializableTypeInfo,
						argSymbol: type,
						definitionSymbol: symbol,
						isExempt: isExempt,
						genericArguments: genericMethodArguments,
						genericBaseArguments: genericBaseTypeArguments );

				} else {
					context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.Descriptor, arg.GetLocation() ) );
				}


			}
		}

		private static (bool, ITypeSymbol? type) CheckIfDeserializableAndUpdateToUnderlyingType(
			OperationAnalysisContext context,
			ref ISymbol? symbol,
			IArgumentOperation? argSymbol,
			Location argLocation,
			TypeInfo typeInfo,
			DeserializableTypeInfo deserializableTypeInfo
		) {
			if( symbol == null ) {
				// check for typeof(T)
				if( symbol == null && argSymbol?.Value is ITypeOfOperation typeOp ) {
					if( DeserializableAnalyzerHelper.IsDeserializable( typeOp.TypeOperand, deserializableTypeInfo ) ) {
						return (true, null);
					}
					symbol = typeOp.TypeOperand;
					// check the result of casting or other non-obvious conversions
				} else if( typeInfo.Type != null
						   && DeserializableAnalyzerHelper.IsDeserializable( typeInfo.Type, deserializableTypeInfo ) ) {
					return (true, null);
				}
			}

			if( symbol == null ) {
				context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.Descriptor, argLocation ) );
				return (true, null);
			}

			// if the MustBeDeserializable attribute is on the definition of the variable in question then it's fine unless it has invalid type arguments
			if( DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( symbol, deserializableTypeInfo ) ) {
				INamedTypeSymbol? typeSymbol = symbol as INamedTypeSymbol;
				if( typeSymbol is not { IsGenericType: true } ) {
					return (true, null);
				}
			}

			// try getting the type from common ways the variable might be assigned
			IParameterSymbol? parameterSymbol = symbol as IParameterSymbol;
			ITypeSymbol? type = typeInfo.Type ?? parameterSymbol?.Type;
			if( symbol is IFieldSymbol fieldSymbol ) {
				type = fieldSymbol.Type;
			} else if( symbol is IMethodSymbol methodParameterSymbol ) {
				type = methodParameterSymbol.ReceiverType ?? methodParameterSymbol.ReturnType;
			}

			return (false, type);
		}

		private static bool ValidateIsDeserializable(
			OperationAnalysisContext context,
			Location location,
			DeserializableTypeInfo deserializableTypeInfo,
			ITypeSymbol argSymbol,
			ISymbol definitionSymbol,
			Func<ITypeSymbol, bool> isExempt,
			IEnumerable<ITypeSymbol>? genericArguments = null,
			IEnumerable<ITypeSymbol>? genericBaseArguments = null
			) {
			Queue<ITypeSymbol> queue = new Queue<ITypeSymbol>();
			queue.Enqueue( argSymbol );
			// only report the dianostic once on the invocation
			bool unsafeArgument = false;

			while( queue.Any() ) {
				ITypeSymbol current = queue.Dequeue();

				// if a generic type parameter check it has the appropriate attribute
				if( current.Kind == SymbolKind.TypeParameter ) {
					// check if defined in the parent method
					if( genericArguments != null ) {
						ITypeSymbol matchingArgument = genericArguments.FirstOrDefault( a => a.Equals( current, SymbolEqualityComparer.Default ) );
						if( matchingArgument != null
							&& !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( current, deserializableTypeInfo ) ) {
							context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.ParameterShouldBeChangedDescriptor, matchingArgument.Locations.First() ) );
						}
					}
					// check if it comes from the base type
					if( genericBaseArguments != null ) {
						ITypeSymbol baseTypeArgument = genericBaseArguments.FirstOrDefault( a => a.Equals( current, SymbolEqualityComparer.Default ) );
						if( baseTypeArgument != null ) {
							if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( current, deserializableTypeInfo ) ) {
								context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.ParameterShouldBeChangedDescriptor, baseTypeArgument.Locations.First() ) );
							}
							if( definitionSymbol is IParameterSymbol && !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( definitionSymbol, deserializableTypeInfo ) ) {
								context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
							}
						}
					}
					// if there's a concrete type that isn't deserializable then mark the location given
				} else if( !unsafeArgument
					&& !isExempt( current )
					&& !DeserializableAnalyzerHelper.IsDeserializable( current, deserializableTypeInfo ) ) {
					if( definitionSymbol is IParameterSymbol ) {
						if( !DeserializableAnalyzerHelper.HasMustBeDeserializableAttribute( definitionSymbol, deserializableTypeInfo ) ) {
							context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.ParameterShouldBeChangedDescriptor, definitionSymbol.Locations.First() ) );
						}
					} else {
						unsafeArgument = true;
						context.ReportDiagnostic( Diagnostic.Create( deserializableTypeInfo.Descriptor, location ) );
					}
				}

				if( current is INamedTypeSymbol genericArgSymbol ) {
					foreach( ITypeSymbol arg in genericArgSymbol.TypeArguments ) {
						queue.Enqueue( arg );
					}
				}
			}

			return unsafeArgument;
		}
	}
}
