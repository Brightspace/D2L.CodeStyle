using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class SingletonLocatorAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.SingletonLocatorMisuse );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterSingletonLocatorAnalyzer );
		}

		public void RegisterSingletonLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var locatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.SingletonLocator" );
			var locatorType2 = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.OldAndBrokenSingletonLocator" );

			// If this type lookup failed then SingletonLocator cannot resolve
			// and we don't need to register our analyzer.
			if( locatorType.IsNullOrErrorType() && locatorType2.IsNullOrErrorType() ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					IsSingletonLocator
				),
				SyntaxKind.SimpleMemberAccessExpression,
				SyntaxKind.InvocationExpression
			);

			bool IsSingletonLocator( INamedTypeSymbol other ) {
				if( !locatorType.IsNullOrErrorType() && other == locatorType ) {
					return true;
				}

				if( !locatorType2.IsNullOrErrorType() && other == locatorType2 ) {
					return true;
				}

				return false;
			}
		}

		// Enforce that SingletonLocator can only load actual [Singleton]s
		private static void EnforceSingletonsOnly(
			SyntaxNodeAnalysisContext context,
			Func<INamedTypeSymbol, bool> isSingletonLocator
		) {
			var root = GetRootNode( context );
			if( root == null ) {
				return;
			}
			var symbolinfo = context.SemanticModel.GetSymbolInfo( root );

			var method = symbolinfo.Symbol as IMethodSymbol;

			if( method == null ) {
				if( symbolinfo.CandidateSymbols == null ) {
					return;
				}

				if( symbolinfo.CandidateSymbols.Length != 1 ) {
					return;
				}

				//This happens on method groups, such as
				//  Func<IFoo> fooFunc = OldAndBrokenServiceLocator.Get<IFoo>;
				method = symbolinfo.CandidateSymbols.First() as IMethodSymbol;

				if( method == null ) {
					return;
				}
			}

			// At this point method is a non-null IMethodSymbol
			if( !isSingletonLocator( method.ContainingType ) ) {
				return;

			}

			if( !IsSingletonGet( method ) ) {
				return;
			}

			//It's ok as long as the attribute is present, error otherwise
			ITypeSymbol typeArg = method.TypeArguments.First();

			if( typeArg.GetFullTypeName() == "D2L.LP.Extensibility.Activation.Domain.IPlugins"
				&& typeArg is INamedTypeSymbol namedTypeArg
				&& namedTypeArg.Arity == 1
			) {
				typeArg = namedTypeArg.TypeArguments.First();
			}

			if( Attributes.Singleton.IsDefined( typeArg ) ) {
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
			if( root != null ) {
				return root;
			}

			root = context.Node as MemberAccessExpressionSyntax;
			if( root == null ) {
				return null;
			}

			if( root.Parent.IsKind( SyntaxKind.InvocationExpression ) ) {
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