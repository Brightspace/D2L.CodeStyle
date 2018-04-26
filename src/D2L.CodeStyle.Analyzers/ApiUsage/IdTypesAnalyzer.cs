using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class IdTypesAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.IdTypeParameterMismatch
		);

		private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> PARAM_TO_VARIABLE_BLACKLIST = new Dictionary<string, string[]> {
			{ "userId", new[] { "orgId", "orgUnitId" } },
			{ "orgId", new[] { "orgUnitId", "userId" } },
			{ "orgUnitId", new[] { "userId" } },
			
		}.ToImmutableDictionary( x => x.Key.ToUpperInvariant(), x => x.Value.Select( y => y.ToUpperInvariant() ).ToImmutableHashSet() );

		private static readonly IImmutableSet<string> INTERESTING_PARAM_NAMES = PARAM_TO_VARIABLE_BLACKLIST.Keys.ToImmutableHashSet();
		private static readonly IImmutableSet<string> INTERESTING_VARAIBLE_NAMES = PARAM_TO_VARIABLE_BLACKLIST.SelectMany( x => x.Value ).ToImmutableHashSet();

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterIdTypedAnalyzer );
		}

		public static void RegisterIdTypedAnalyzer( CompilationStartAnalysisContext context ) {
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation( ctx, ( InvocationExpressionSyntax )ctx.Node ),
				SyntaxKind.InvocationExpression
			);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext ctx,
			InvocationExpressionSyntax invocation	
		) {
			ArgumentListSyntax argumentList = invocation.ArgumentList;
			if( argumentList == null ) {
				return;
			}

			SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;
			foreach( var argument in arguments ) {
				IdentifierNameSyntax argumentExpression = argument.Expression as IdentifierNameSyntax;
				if( argumentExpression == null ) {
					continue;
				}

				string variableName = argumentExpression.Identifier.Text;
				string variableNameNormalized = variableName.ToUpperInvariant();
				if( !INTERESTING_VARAIBLE_NAMES.Contains( variableNameNormalized ) ) {
					continue;
				}

				IParameterSymbol parameter = argument.DetermineParameter(
					ctx.SemanticModel,
					allowParams: true
				);
				if( parameter.IsNullOrErrorType() ) {
					continue;
				}

				string parameterName = parameter.Name;
				string parameterNameNormalized = parameterName.ToUpperInvariant();
				if ( !INTERESTING_PARAM_NAMES.Contains( parameterNameNormalized ) ) {
					continue;
				}

				if( !PARAM_TO_VARIABLE_BLACKLIST[parameterNameNormalized].Contains( variableNameNormalized ) ) {
					continue;
				}

				ctx.ReportDiagnostic( Diagnostic.Create( Diagnostics.IdTypeParameterMismatch, argumentExpression.GetLocation() ) );
			}
		}
	}
}
