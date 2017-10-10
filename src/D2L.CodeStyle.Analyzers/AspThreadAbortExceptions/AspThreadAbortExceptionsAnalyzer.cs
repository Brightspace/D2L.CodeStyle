using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.AspThreadAbortExceptions {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class AspThreadAbortExceptionsAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.UnsafeUseOfAspRedirect,
				Diagnostics.DontUseAspResponseEnd
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		public static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			var requestType = context.Compilation.GetTypeByMetadataName(
				"System.Web.HttpResponse"
			);

			if ( requestType == null ) {
				return;
			}

			var methods = requestType.GetMembers()
				.OfType<IMethodSymbol>().ToImmutableArray();

			var evilRedirectMethod = methods
				.First( m => m.Name == "Redirect" && m.Parameters.Length == 1 );

			var lessEvilRedirectMethod = methods
				.First( m => m.Name == "Redirect" && m.Parameters.Length == 2 );

			var endMethod = methods
				.First( m => m.Name == "End" );

			var endResponseArg = lessEvilRedirectMethod.Parameters
				.First( p => p.Name == "endResponse" );

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation(
					ctx,
					endMethod: endMethod,
					evilRedirectMethod: evilRedirectMethod,
					lessEvilRedirectMethod: lessEvilRedirectMethod,
					endResponseArg: endResponseArg
				),
				SyntaxKind.InvocationExpression
			);
		}

		public static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			IMethodSymbol endMethod,
			IMethodSymbol evilRedirectMethod,
			IMethodSymbol lessEvilRedirectMethod,
			IParameterSymbol endResponseArg
		) {
			var node = (InvocationExpressionSyntax)context.Node;
			var memberAccess = node.Expression as MemberAccessExpressionSyntax;

			if ( memberAccess == null ) {
				return;
			}

			var invokedMethod = context.SemanticModel.GetSymbolInfo( memberAccess ).Symbol as IMethodSymbol;

			if ( invokedMethod == endMethod ) {
				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.DontUseAspResponseEnd,
					node.GetLocation()
				));

				return;
			}

			if ( invokedMethod == evilRedirectMethod ) {
				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.UnsafeUseOfAspRedirect,
					node.GetLocation()
				));
			}

			if( invokedMethod == lessEvilRedirectMethod ) {
				// Respose.Redirect( x, false ) is ok... see if that's what we've got

				var endResponseExpr = GetEndResponseArgument(
					context.SemanticModel,
					node
				).Expression as LiteralExpressionSyntax;

				if( endResponseExpr != null && endResponseExpr.Token.Kind() == SyntaxKind.FalseKeyword ) {
					return;
				}

				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.UnsafeUseOfAspRedirect,
					node.GetLocation()
				) );
			}
		}

		// Deal with parameters carefully. Really need to figure out how to
		// avoid this stuff. The IOperation functionality looks very promising
		// for dealing with things at a more abstract level. A prototype
		// implementation is far simpler but that functionality is feature-toggled
		// off for now.
		private static ArgumentSyntax GetEndResponseArgument(
			SemanticModel model,
			InvocationExpressionSyntax invocation
		) {
			var firstArg = invocation.ArgumentList.Arguments[0];
			var secondArg = invocation.ArgumentList.Arguments[1];
			var typeOfFirstArg = firstArg.DetermineParameter( model ).Type;

			// This could get confused with implicit casts but that should fail
			// loudly and I wouldn't feel bad.
			if ( typeOfFirstArg.SpecialType == SpecialType.System_Boolean ) {
				return firstArg;
			} else {
				return secondArg;
			}
		}
	}
}
