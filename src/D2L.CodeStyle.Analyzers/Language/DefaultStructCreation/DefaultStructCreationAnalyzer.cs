using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Language.DefaultStructCreation.Replacements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Language.DefaultStructCreation {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed partial class DefaultStructCreationAnalyzer : DiagnosticAnalyzer {

		internal const string ACTION_TITLE_KEY = nameof( DefaultStructCreationAnalyzer ) + ".ActionTitle";
		internal const string REPLACEMENT_KEY = nameof( DefaultStructCreationAnalyzer ) + ".Replacement";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.DontCallDefaultStructConstructor );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();

			context.ConfigureGeneratedCodeAnalysis(
				GeneratedCodeAnalysisFlags.None
			);

			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			var replacers = ImmutableArray.Create(
				NewGuidBackedIdTypeReplacer.Instance,
				new NewGuidReplacer( context.Compilation ),
				new NewImmutableArrayReplacer( context.Compilation )
			);

			context.RegisterOperationAction(
				ctx => AnalyzeObjectCreation(
					context: ctx,
					operation: ctx.Operation as IObjectCreationOperation,
					replacers: replacers
				),
				OperationKind.ObjectCreation
			);
		}

		private static void AnalyzeObjectCreation(
			OperationAnalysisContext context,
			IObjectCreationOperation operation,
			ImmutableArray<IDefaultStructCreationReplacer> replacers
		) {
			if( operation.Type.TypeKind != TypeKind.Struct ) {
				return;
			}

			IMethodSymbol constructor = operation.Constructor;
			if( constructor.Parameters.Length > 0 ) {
				return;
			}

			INamedTypeSymbol structType = operation.Type as INamedTypeSymbol;

			foreach( var replacer in replacers ) {

				if( !replacer.CanReplace( structType ) ) {
					continue;
				}

				var syntax = operation.Syntax as ObjectCreationExpressionSyntax;
				SyntaxNode replacement = replacer
					.GetReplacement( structType, syntax.Type )
					.WithTriviaFrom( syntax );

				context.ReportDiagnostic( Diagnostic.Create(
					descriptor: Diagnostics.DontCallDefaultStructConstructor,
					location: operation.Syntax.GetLocation(),
					properties: ImmutableDictionary<string, string>
						.Empty
						.Add( ACTION_TITLE_KEY, replacer.Title )
						.Add( REPLACEMENT_KEY, replacement.ToFullString() ),
					structType.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat )
				) );

				break;
			}
		}
	}
}