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

			bool isConfigured = false;

			ITypeSymbol invocationTypeInfo =
				context.SemanticModel.GetTypeInfo( ((AwaitExpressionSyntax) context.Node).Expression ).Type;

			if( invocationTypeInfo != null ) {
				isConfigured = invocationTypeInfo.OriginalDefinition.Equals( nonGenericAwaitable ) ||
				               invocationTypeInfo.OriginalDefinition.Equals( genericAwaitable );
			}

			if( !isConfigured ) {

				var diagnostic = Diagnostic.Create( s_rule, context.Node.GetLocation() );

				context.ReportDiagnostic( diagnostic );
			}
		}

	}

}