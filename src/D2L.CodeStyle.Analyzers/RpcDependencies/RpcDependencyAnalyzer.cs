using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.RpcDependencies {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class RpcDependencyAnalyzer : DiagnosticAnalyzer {
		internal static readonly DiagnosticDescriptor RpcContextRule = new DiagnosticDescriptor(
			id: "D2L0004",
			title: "RPCs must take an IRpcContext, IRpcPostContext or IRpcPostContextBase as their first argument",
			messageFormat: "RPCs must take an IRpcContext, IRpcPostContext or IRpcPostContextBase as their first argument",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "RPCs must take an IRpcContext, IRpcPostContext or IRpcPostContextBase as their first argument"
		);

		internal static readonly DiagnosticDescriptor SortRule = new DiagnosticDescriptor(
			id: "D2L0005",
			title: "Dependency-injected arguments in RPC methods must preceed other parameters (other than the first context argument)",
			messageFormat: "Dependency-injected arguments in RPC methods must preceed other parameters (other than the first context argument)",
			category: "Correctness",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Dependency-injected arguments in RPC methods must preceed other parameters (other than the first context argument)"
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( RpcContextRule, SortRule );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterRpcAnalyzer );
		}

		public static void RegisterRpcAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var rpcAttributeType = context.Compilation.GetTypeByMetadataName( "D2L.Web.RpcAttribute" );
			var rpcContextType = context.Compilation.GetTypeByMetadataName( "D2L.Web.IRpcContext" );
			var rpcPostContextType = context.Compilation.GetTypeByMetadataName( "D2L.Web.IRpcPostContext" );
			var rpcPostContextBaseType = context.Compilation.GetTypeByMetadataName( "D2L.Web.RequestContext.IRpcPostContextBase" );
			var dependencyAttributeType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.DependencyAttribute" );

			// If any of those type lookups failed then presumably D2L.Web is
			// not referenced and we don't need to register our analyzer.

			if ( rpcAttributeType == null || rpcAttributeType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if ( rpcContextType == null || rpcContextType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if ( rpcPostContextType == null || rpcPostContextType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if ( rpcPostContextBaseType == null || rpcPostContextBaseType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			// intentionally not checking dependencyAttributeType: still want
			// the analyzer to run if that is not in scope.

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeMethod(
					ctx,
					rpcAttributeType: rpcAttributeType,
					rpcContextType: rpcContextType,
					rpcPostContextType: rpcPostContextType,
					rpcPostContextBaseType: rpcPostContextBaseType,
					dependencyAttributeType: dependencyAttributeType
				),
				SyntaxKind.MethodDeclaration
			);
		}

		private static void AnalyzeMethod(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol rpcAttributeType,
			INamedTypeSymbol rpcContextType,
			INamedTypeSymbol rpcPostContextType,
			INamedTypeSymbol rpcPostContextBaseType,
			INamedTypeSymbol dependencyAttributeType
		) {
			var method = context.Node as MethodDeclarationSyntax;

			if ( method == null ) {
				return;
			}

			bool keepGoing = CheckThatFirstArgumentIsIRpcContext(
				context,
				method,
				rpcAttributeType: rpcAttributeType,
				rpcContextType: rpcContextType,
				rpcPostContextType: rpcPostContextType,
				rpcPostContextBaseType: rpcPostContextBaseType
			);

			if ( !keepGoing ) {
				return;
			}

			CheckThatDependencyArgumentsAreSortedCorrectly(
				context,
				method.ParameterList.Parameters,
				dependencyAttributeType
			);

			// other things to check:
			// - appropriate use of [Dependency]
		}

		private static bool CheckThatFirstArgumentIsIRpcContext(
			SyntaxNodeAnalysisContext context,
			MethodDeclarationSyntax method,
			INamedTypeSymbol rpcAttributeType,
			INamedTypeSymbol rpcContextType,
			INamedTypeSymbol rpcPostContextType,
			INamedTypeSymbol rpcPostContextBaseType
		) {
			bool isRpc = method
				.AttributeLists
				.SelectMany( al => al.Attributes )
				.Any( attr => IsAttribute( rpcAttributeType, attr, context.SemanticModel ) );

			if( !isRpc ) {
				return false;
			}

			var ps = method.ParameterList.Parameters;

			if( ps.Count == 0 ) {
				context.ReportDiagnostic(
					Diagnostic.Create( RpcContextRule, method.ParameterList.GetLocation() )
				);
				return false;
			}

			var firstParam = method.ParameterList.Parameters[0];

			var firstParamIsReasonableType =
				ParameterIsOfType( rpcContextType, firstParam, context.SemanticModel ) ||
				ParameterIsOfType( rpcPostContextType, firstParam, context.SemanticModel ) ||
				ParameterIsOfType( rpcPostContextBaseType, firstParam, context.SemanticModel );

			if( !firstParamIsReasonableType ) {
				context.ReportDiagnostic(
					Diagnostic.Create( RpcContextRule, firstParam.GetLocation() )
				);
			}

			return true;
		}

		private static void CheckThatDependencyArgumentsAreSortedCorrectly(
			SyntaxNodeAnalysisContext context,
			SeparatedSyntaxList<ParameterSyntax> ps,
			INamedTypeSymbol dependencyAttributeType
		) {
			bool doneDependencies = false;
			foreach( var param in ps.Skip( 1 ) ) {
				var isDep = param
					.AttributeLists
					.SelectMany( al => al.Attributes )
					.Any( attr => IsAttribute( dependencyAttributeType, attr, context.SemanticModel ) );

				if( !isDep && !doneDependencies ) {
					doneDependencies = true;
				} else if( isDep && doneDependencies ) {
					context.ReportDiagnostic( Diagnostic.Create( SortRule, param.GetLocation() ) );
				}
			}
		}

		private static bool IsAttribute(
			INamedTypeSymbol expectedType,
			AttributeSyntax attr,
			SemanticModel model
		) {
			var symbol = model.GetSymbolInfo( attr ).Symbol;

			if( symbol == null || symbol.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			// Note: symbol corresponds to the constructor for the attribute,
			// so we need to look at symbol.ContainingType
			return symbol.ContainingType.Equals( expectedType );
		}

		private static bool ParameterIsOfType(
			INamedTypeSymbol expectedType,
			ParameterSyntax param,
			SemanticModel model
		) {
			var symbol = model.GetSymbolInfo( param.Type ).Symbol;

			if ( symbol == null || symbol.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			return symbol.Equals( expectedType );
		}
	}
}