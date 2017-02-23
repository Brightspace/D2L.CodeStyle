using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

namespace D2L.CodeStyle.Analyzers.Threading {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class UseConfigureAwaitFalseAnalyzer : DiagnosticAnalyzer {

		public const string DiagnosticId = "D2L0004";
		private const string CATEGORY = "Naming";

		private static readonly LocalizableString s_title = "Awaitable should specify a ConfigureAwait";
		private static readonly LocalizableString s_description = "Awaitable should use ConfigureAwait(false) if possible";

		private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
			DiagnosticId,
			s_title,
			s_description,
			CATEGORY,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: s_description );

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get { return ImmutableArray.Create<DiagnosticDescriptor>( s_rule ); }
		}


		public override void Initialize( AnalysisContext context ) {
			context.RegisterSyntaxNodeAction( AnalyzeSymbol, SyntaxKind.AwaitExpression );
		}

		private static void AnalyzeSymbol( SyntaxNodeAnalysisContext context ) {
			INamedTypeSymbol nonGenericAwaitable =
				context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable" );
			INamedTypeSymbol genericAwaitable =
				context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1" );

			bool isConfigured = context.Node.DescendantNodes()
			                           .OfType<MemberAccessExpressionSyntax>()
			                           .Any(
				                           x => IsConfigureAwaitFunction(
					                           x,
					                           context.SemanticModel,
					                           genericAwaitable,
					                           nonGenericAwaitable ) );

			if( !isConfigured ) {

				var diagnostic = Diagnostic.Create( s_rule, context.Node.GetLocation() );

				context.ReportDiagnostic( diagnostic );
			}
		}

		private static bool IsConfigureAwaitFunction(
			MemberAccessExpressionSyntax node,
			SemanticModel model,
			params INamedTypeSymbol[] awaitableSymbols ) {
			if( !node.IsKind( SyntaxKind.SimpleMemberAccessExpression ) ) {
				return false;
			}

			ISymbol symbol = model.GetSymbolInfo( node ).Symbol;

			if( symbol == null ) {
				return false;
			}

			IMethodSymbol methodSymbol = symbol as IMethodSymbol;

			if( methodSymbol == null || methodSymbol.ReturnsVoid ) {
				return false;
			}

			var namedType = methodSymbol.ReturnType as INamedTypeSymbol;

			if( namedType == null ) {
				return false;
			}

			return awaitableSymbols.Any( x => namedType.OriginalDefinition.Equals( x ) );
		}

	}

}