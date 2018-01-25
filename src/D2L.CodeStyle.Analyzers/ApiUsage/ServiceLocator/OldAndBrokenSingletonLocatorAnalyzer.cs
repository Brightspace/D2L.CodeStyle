using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class OldAndBrokenSingletonLocatorAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.SingletonLocatorMisuse );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterSingletonLocatorAnalyzer );
		}

		public void RegisterSingletonLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var locatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenSingletonLocator" );

			// If this type lookup failed then OldAndBrokenSingletonLocator
			// cannot resolve and we don't need to register our analyzer.
			if( locatorType.IsNullOrErrorType() ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					locatorType
				),
				SyntaxKind.SimpleMemberAccessExpression,
				SyntaxKind.InvocationExpression
			);
		}

		//Enforce that OldAndBrokenSingletonLocator can only load actual Singletons
		private static void EnforceSingletonsOnly(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol singletonLocatorType
		) {
			var root = GetRootNode( context );
			if( root == null ) {
				return;
			}
			var symbolinfo = context.SemanticModel.GetSymbolInfo( root );
			var method = symbolinfo.Symbol as IMethodSymbol;
			if( method == null ) {
				if( symbolinfo.CandidateSymbols != null && symbolinfo.CandidateSymbols.Length == 1 ) {
					//This happens on method groups, such as
					//  Func<IFoo> fooFunc = OldAndBrokenServiceLocator.Get<IFoo>;
					method = symbolinfo.CandidateSymbols.First() as IMethodSymbol;
				} else {
					return;
				}
			}

			if( !singletonLocatorType.Equals(method.ContainingType) ) {
				return;
			}

			if( !IsSingletonGet( method ) ) {
				return;
			}
			
			//It's ok as long as the attribute is present, error otherwise
			ITypeSymbol typeArg = method.TypeArguments.First();
			if ( Attributes.Singleton.IsDefined( typeArg ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create( Diagnostics.SingletonLocatorMisuse, context.Node.GetLocation(), typeArg.GetFullTypeName() )
			);
		}

		private static ExpressionSyntax GetRootNode( SyntaxNodeAnalysisContext context ) {
			//It turns out that depending on how you call the locator, it might contain any of:
			//* a SimpleMemberAccessExpression
			//* an InvocationExpression
			//* an InvocationExpression wrapped around a SimpleMemberAccessExpression
			//We want to count each of these as a single error (avoid double counting the last case)
			ExpressionSyntax root = context.Node as InvocationExpressionSyntax;
			if (root != null) {
				return root;
			}

			root = context.Node as MemberAccessExpressionSyntax;
			if (root == null) {
				return null;
			}

			if( root.Parent.IsKind(SyntaxKind.InvocationExpression) ) {
				return null;
			}
			return root;
		}

		private static bool IsSingletonGet( IMethodSymbol method ) {
			return "Get".Equals( method.Name )
				&& method.IsGenericMethod
				&& method.TypeArguments.Length == 1;
		}

	}
}