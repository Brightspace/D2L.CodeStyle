using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class NoAsyncFuncsAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.AsyncFuncsBlocked
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private static void OnCompilationStart( CompilationStartAnalysisContext context ) {
			INamedTypeSymbol? NoAsyncFuncsAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.CodeStyle.Annotations.Contract.NoAsyncFuncsAttribute"
			);

			if( NoAsyncFuncsAttribute.IsNullOrErrorType() ) {
				return;
			}

			if( !AwaitabilityContext.TryCreate( context.Compilation, out AwaitabilityContext? awaitabilityContext ) ) {
				return;
			}

			context.RegisterOperationAction(
				ctx => AnalyzeArgument(
					context: ctx,
					NoAsyncFuncsAttribute,
					awaitabilityContext,
					operation: (IArgumentOperation)ctx.Operation
				),
				OperationKind.Argument
			);
		}

		private static void AnalyzeArgument(
			OperationAnalysisContext context,
			INamedTypeSymbol NoAsyncFuncsAttribute,
			AwaitabilityContext awaitabilityContext,
			IArgumentOperation operation
		) {
			if( operation.SemanticModel is null ) {
				return;
			}

			if( operation.Parameter is null ) {
				return;
			}

			AttributeData? noAsyncFuncs = operation
				.Parameter
				.GetAttributes()
				.FirstOrDefault( a => SymbolEqualityComparer.Default.Equals( NoAsyncFuncsAttribute, a.AttributeClass ) );
			if( noAsyncFuncs is null ) {
				return;
			}

			ImmutableArray<ITypeSymbol> potentialReturnTypes = GetReturnTypesToCheck(
				operation.SemanticModel,
				operation,
				context.CancellationToken
			);
			foreach( ITypeSymbol returnType in potentialReturnTypes ) {
				if( awaitabilityContext.IsAwaitable( returnType ) ) {
					ReportDiagnostic(
						context,
						operation,
						noAsyncFuncs
					);
					return;
				}
			}
		}

		private static ImmutableArray<ITypeSymbol> GetReturnTypesToCheck(
			SemanticModel model,
			IArgumentOperation actionArgument,
			CancellationToken ct
		) {
			SymbolInfo argumentSymbolInfo = model.GetSymbolInfo(
				actionArgument.Value.Syntax,
				ct
			);

			if( argumentSymbolInfo.Symbol is IMethodSymbol argMethodSymbol ) {
				return ImmutableArray.Create( argMethodSymbol.ReturnType.OriginalDefinition );
			}

			return argumentSymbolInfo
				.CandidateSymbols
				.Where( s => s.Kind == SymbolKind.Method )
				.Cast<IMethodSymbol>()
				.Select( x => x.ReturnType.OriginalDefinition )
				.ToImmutableArray();
		}

		private static void ReportDiagnostic(
			OperationAnalysisContext context,
			IArgumentOperation operation,
			AttributeData attribute
		) {
			string? message = null;
			foreach( var namedArg in attribute.NamedArguments ) {
				if( namedArg.Key == "Message" ) {
					message = (string)namedArg.Value.Value!;
					message = ". " + message;
					break;
				}
			}

			context.ReportDiagnostic(
				Diagnostics.AsyncFuncsBlocked,
				operation.Syntax.GetLocation(),
				messageArgs: new[] { message }
			);
		}

		private sealed class AwaitabilityContext {

			private readonly ImmutableHashSet<INamedTypeSymbol> m_taskTypeBuiltins;
			private readonly INamedTypeSymbol? m_asyncMethodBuilderAttribute;

			private AwaitabilityContext(
				ImmutableHashSet<INamedTypeSymbol> taskTypeBuiltins,
				INamedTypeSymbol? asyncMethodBuilderAttribute
			) {
				m_taskTypeBuiltins = taskTypeBuiltins;
				m_asyncMethodBuilderAttribute = asyncMethodBuilderAttribute;
			}

			public bool IsAwaitable( ITypeSymbol? type ) {
				if( type is not INamedTypeSymbol namedType ) {
					return false;
				}

				namedType = namedType.OriginalDefinition;

				if( m_taskTypeBuiltins.Contains( namedType ) ) {
					return true;
				}

				if( m_asyncMethodBuilderAttribute is null ) {
					return false;
				}

				bool typeIsCustomAwaitable = namedType
					.GetAttributes()
					.Any( a => SymbolEqualityComparer.Default.Equals( m_asyncMethodBuilderAttribute, a.AttributeClass ) );

				return typeIsCustomAwaitable;
			}

			public static bool TryCreate(
				Compilation compilation,
				[NotNullWhen( true )] out AwaitabilityContext? context
			) {
				ImmutableHashSet<INamedTypeSymbol>.Builder taskTypeBuiltins
					= ImmutableHashSet.CreateBuilder<INamedTypeSymbol>( SymbolEqualityComparer.Default );

				AddTaskTypeIfPresent( "System.Threading.Tasks.Task" );
				AddTaskTypeIfPresent( "System.Threading.Tasks.Task`1" );
				AddTaskTypeIfPresent( "System.Threading.Tasks.ValueTask" );
				AddTaskTypeIfPresent( "System.Threading.Tasks.ValueTask`1" );
				AddTaskTypeIfPresent( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable" );
				AddTaskTypeIfPresent( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1" );

				// [AsyncMethodBuilder] is used to create custom awaitable types
				// See https://blogs.msdn.microsoft.com/seteplia/2018/01/11/extending-the-async-methods-in-c/
				INamedTypeSymbol? AsyncMethodBuilderAttribute = compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute" );

				if( taskTypeBuiltins.Count == 0 && AsyncMethodBuilderAttribute.IsNullOrErrorType() ) {
					context = default;
					return false;
				}

				context = new(
					taskTypeBuiltins: taskTypeBuiltins.ToImmutable(),
					asyncMethodBuilderAttribute: AsyncMethodBuilderAttribute.IsNullOrErrorType() ? null : AsyncMethodBuilderAttribute
				);
				return true;

				void AddTaskTypeIfPresent( string metadataName ) {
					INamedTypeSymbol? type = compilation.GetTypeByMetadataName( metadataName );
					if( type.IsNullOrErrorType() ) {
						return;
					}

					taskTypeBuiltins.Add( type );
				}
			}

		}

	}
}
