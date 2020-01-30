using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.UnnecessaryParameters {
	internal partial class UnnecessaryParametersAnalyzer  {

		[ExportCodeFixProvider(
			LanguageNames.CSharp,
			Name = nameof( UnnecessaryParametersCodefix )
		)]
		public sealed class UnnecessaryParametersCodefix : CodeFixProvider {

			public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
				Diagnostics.ParametersShouldBeRemoved.Id
			);

			public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

			public override async Task RegisterCodeFixesAsync( CodeFixContext context ) {
                
				var root = await context.Document
					.GetSyntaxRootAsync( context.CancellationToken )
					.ConfigureAwait( false );

				foreach( var diagnostic in context.Diagnostics ) {
					var span = diagnostic.Location.SourceSpan;

					SyntaxNode syntax = root.FindNode( span, getInnermostNodeForTie: true );
					if( !( syntax is MethodDeclarationSyntax declaration ) ) {
						continue;
					}

					context.RegisterCodeFix(
						CodeAction.Create(
							title: "Remove Unnecessary Parameters",
							ct => RemoveParameters(
								doc: context.Document,
								root: root,
								declaration: declaration,
								ct: ct
							)
						),
						diagnostic
					);
				}
			}

			private async static Task<Document> RemoveParameters(
					Document doc,
					SyntaxNode root,
					MethodDeclarationSyntax declaration,
					CancellationToken ct
			) {

				ParameterListSyntax parameterList = declaration.ParameterList;
				SeparatedSyntaxList<ParameterSyntax> list = UpdateParametersList( declaration, parameterList );
				parameterList = parameterList.WithParameters( list );

				root = root.ReplaceNode( declaration, declaration.WithParameterList( parameterList ) );
				doc = doc.WithSyntaxRoot( root );
				return doc;
			}

			private static SeparatedSyntaxList<ParameterSyntax> UpdateParametersList(
				MethodDeclarationSyntax declaration,
				ParameterListSyntax parameterList
			) {

				string methodsName = declaration.Identifier.Text;
				SeparatedSyntaxList<ParameterSyntax> list = parameterList.Parameters;

				switch( methodsName ) {
					case "HasPermission":
						list.RemoveAt( list.Count - 1 );
						list.RemoveAt( list.Count - 1 );
						break;
					case "HasCapability":
						list.RemoveAt( list.Count - 1 );
						list.RemoveAt( list.Count - 1 );
						break;
					default:
						break;
				}

				return list;
			}
		}
	}
}
