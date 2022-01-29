using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Logging {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class LoggingExecutionContextScopeBuilderAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.LoggingContextRunAwaitable
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterScopeBuilderAnalyzer );
		}

		private void RegisterScopeBuilderAnalyzer( CompilationStartAnalysisContext context ) {
			var ILoggingExecutionContextScopeBuilder = context.Compilation.GetTypeByMetadataName( "D2L.LP.Logging.ExecutionContexts.ILoggingExecutionContextScopeBuilder" );

			// ILoggingExecutionContextScopeBuilder not in compilation, no need to register
			if( ILoggingExecutionContextScopeBuilder == null || ILoggingExecutionContextScopeBuilder.Kind == SymbolKind.ErrorType ) {
				return;
			}

			ImmutableHashSet<ISymbol> ILoggingExecutionContextScopeBuilderRunSymbols = ILoggingExecutionContextScopeBuilder
				.GetMembers()
				.Where( m => m.Kind == SymbolKind.Method )
				.Where( m => m.MetadataName == "Run" )
				.ToImmutableHashSet( SymbolEqualityComparer.Default );

			// Can't find ILoggingExecutionContextScopeBuilder.Run or ILoggingExecutionContextScopeBuilder.Run<T>
			if( !ILoggingExecutionContextScopeBuilderRunSymbols.Any() ) {
				return;
			}

			ImmutableHashSet<ISymbol> taskTypeBuiltins = 
				new[] {
					context.Compilation.GetTypeByMetadataName( "System.Threading.Tasks.Task" ),
					context.Compilation.GetTypeByMetadataName( "System.Threading.Tasks.Task`1" ),
					context.Compilation.GetTypeByMetadataName( "System.Threading.Tasks.ValueTask" ),
					context.Compilation.GetTypeByMetadataName( "System.Threading.Tasks.ValueTask`1" ),
					context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable" ),
					context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1" )
				}
				.Where( x => x != null && x.Kind != SymbolKind.ErrorType )
				.ToImmutableHashSet( SymbolEqualityComparer.Default );

			// [AsyncMethodBuilder] is used to create custom awaitable types
			// See https://blogs.msdn.microsoft.com/seteplia/2018/01/11/extending-the-async-methods-in-c/
			INamedTypeSymbol AsyncMethodBuilderAttribute = context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute" );

			context.RegisterSyntaxNodeAction(
				ctx => RunInvocationAnalysis(
					context: ctx,
					AsyncMethodBuilderAttribute: AsyncMethodBuilderAttribute,
					ILoggingExecutionContextScopeBuilderRunSymbols: ILoggingExecutionContextScopeBuilderRunSymbols,
					taskTypeBuiltins: taskTypeBuiltins,
					invocationSyntax: ctx.Node as InvocationExpressionSyntax
				),
				SyntaxKind.InvocationExpression
			);
		}

		private void RunInvocationAnalysis(
			SyntaxNodeAnalysisContext context,
			IImmutableSet<ISymbol> ILoggingExecutionContextScopeBuilderRunSymbols,
			INamedTypeSymbol AsyncMethodBuilderAttribute,
			IImmutableSet<ISymbol> taskTypeBuiltins,
			InvocationExpressionSyntax invocationSyntax
		) {
			SemanticModel model = context.SemanticModel;

			if( !IsRunInvocation(
				model,
				ILoggingExecutionContextScopeBuilderRunSymbols,
				invocationSyntax,
				context.CancellationToken
			) ) {
				return;
			}

			if( !TryGetActionArgument(
				invocationSyntax,
				out ArgumentSyntax actionArgument
			) ) {
				return;
			}

			ImmutableArray<ITypeSymbol> potentialReturnTypes = GetReturnTypesToCheck(
				model,
				actionArgument,
				context.CancellationToken
			);
			foreach( ITypeSymbol returnType in potentialReturnTypes ) {
				if( IsAwaitable(
					taskTypeBuiltins,
					AsyncMethodBuilderAttribute,
					returnType
				) ) {
					ReportDiagnostic(
						context: context,
						invocationSyntax: invocationSyntax
					);
					return;
				}
			}
		}

		private static bool IsRunInvocation(
			SemanticModel model,
			IImmutableSet<ISymbol> ILoggingExecutionContextScopeBuilderRunSymbols,
			InvocationExpressionSyntax invocationSyntax,
			CancellationToken ct
		) {
			SymbolInfo methodSymbolInfo = model.GetSymbolInfo(
				invocationSyntax,
				ct
			);

			if( methodSymbolInfo.Symbol != null ) {
				if( ILoggingExecutionContextScopeBuilderRunSymbols.Contains(
					methodSymbolInfo.Symbol.OriginalDefinition
				) ) {
					return true;
				}

				return false;
			}

			if( methodSymbolInfo
				.CandidateSymbols
				.Select( s => s.OriginalDefinition )
				.Any( ILoggingExecutionContextScopeBuilderRunSymbols.Contains )
			) {
				return true;
			}

			return false;
		}

		private static bool TryGetActionArgument(
			InvocationExpressionSyntax invocationSyntax,
			out ArgumentSyntax actionArgument
		) {
			var arguments = invocationSyntax.ArgumentList.Arguments;
			if( arguments.Count != 1 ) {
				actionArgument = default;
				return false;
			}

			actionArgument = arguments.First();
			return true;
		}

		private static ImmutableArray<ITypeSymbol> GetReturnTypesToCheck(
			SemanticModel model,
			ArgumentSyntax actionArgument,
			CancellationToken ct
		) {
			SymbolInfo argumentSymbolInfo = model.GetSymbolInfo(
				actionArgument.Expression,
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

		private static bool IsAwaitable(
			IImmutableSet<ISymbol> taskTypeBuiltins,
			INamedTypeSymbol AsyncMethodBuilderAttribute,
			ITypeSymbol type
		) {
			type = type.OriginalDefinition;

			if( taskTypeBuiltins.Contains( type ) ) {
				return true;
			}

			IEnumerable<AttributeData> typeAttributes = type.GetAttributes();
			bool typeIsCustomAwaitable = typeAttributes.Any(
				a => a.AttributeClass.OriginalDefinition.Equals( AsyncMethodBuilderAttribute, SymbolEqualityComparer.Default )
			);

			if( typeIsCustomAwaitable ) {
				return true;
			}

			return false;
		}

		private static void ReportDiagnostic(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocationSyntax
		) {
			ExpressionSyntax syntaxToLocate = invocationSyntax.Expression;
			if( syntaxToLocate is MemberAccessExpressionSyntax memberAccessSyntax ) {
				syntaxToLocate = memberAccessSyntax.Name;
			}

			context.ReportDiagnostic( Diagnostic.Create(
				Diagnostics.LoggingContextRunAwaitable,
				syntaxToLocate.GetLocation()
			) );
		}

	}
}
