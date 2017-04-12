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

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( RpcContextRule );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterRpcAnalyzer );
		}

		public static void RegisterRpcAnalyzer( CompilationStartAnalysisContext context ) {
			var rpcAttributeType = context.Compilation.GetTypeByMetadataName( "D2L.Web.RpcAttribute" );
			var rpcContextType = context.Compilation.GetTypeByMetadataName( "D2L.Web.IRpcContext" );
			var rpcPostContextType = context.Compilation.GetTypeByMetadataName( "D2L.Web.IRpcPostContext" );
			var rpcPostContextBaseType = context.Compilation.GetTypeByMetadataName( "D2L.Web.RequestContext.IRpcPostContextBase" );

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

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeMethod(
					ctx,
					rpcAttributeType: rpcAttributeType,
					rpcContextType: rpcContextType,
					rpcPostContextType: rpcPostContextType,
					rpcPostContextBaseType: rpcPostContextBaseType
				),
				SyntaxKind.MethodDeclaration
			);
		}

		private static void AnalyzeMethod(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol rpcAttributeType,
			INamedTypeSymbol rpcContextType,
			INamedTypeSymbol rpcPostContextType,
			INamedTypeSymbol rpcPostContextBaseType
		) {
			var method = context.Node as MethodDeclarationSyntax;

			if ( method == null ) {
				return;
			}

			bool isRpc = method
				.AttributeLists
				.SelectMany( al => al.Attributes )
				.Any( attr => IsRpcAttribute( rpcAttributeType, attr, context.SemanticModel ) );

			if( !isRpc ) {
				return;
			}

			var ps = method.ParameterList.Parameters;

			if ( ps.Count == 0 ) {
				context.ReportDiagnostic(
					Diagnostic.Create( RpcContextRule, method.ParameterList.GetLocation() )
				);
				return;
			} else {
				var firstParam = method.ParameterList.Parameters[0];

				var firstParamIsReasonableType =
					ParameterIsOfTypeIRpcContext( rpcContextType, firstParam, context.SemanticModel ) ||
					ParameterIsOfTypeIRpcContext( rpcPostContextType, firstParam, context.SemanticModel ) ||
					ParameterIsOfTypeIRpcContext( rpcPostContextBaseType, firstParam, context.SemanticModel );

				if ( !firstParamIsReasonableType ) {
					context.ReportDiagnostic(
						Diagnostic.Create( RpcContextRule, firstParam.GetLocation() )
					);
				}
			}

			// other things to check:
			// - sort order of [Dependency] arguments
			// - appropriate use of [Dependency]
		}

		private static bool IsRpcAttribute(
			INamedTypeSymbol expectedType,
			AttributeSyntax attr,
			SemanticModel model
		) {
			var symbol = model.GetSymbolInfo( attr ).Symbol;

			if ( symbol == null || symbol.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			// Note: symbol corresponds to the constructor for the attribute,
			// so we need to look at symbol.ContainingType
			return symbol.ContainingType.Equals( expectedType );
		}

		private static bool ParameterIsOfTypeIRpcContext(
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