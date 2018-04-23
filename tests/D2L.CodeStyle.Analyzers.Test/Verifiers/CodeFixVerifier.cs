using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Verifiers {
	internal abstract class CodeFixVerifier : DiagnosticVerifier {

		protected abstract CodeFixProvider GetCSharpCodeFixProvider();

		protected void VerifyCSharpCodeFix(
			string source,
			string expectedFixedSource
		) {
			var fixedSource = ApplyCodeFixesRecursive(
				source
			);

			Assert.AreEqual( expectedFixedSource, fixedSource );
		}

		private string ApplyCodeFixesRecursive(
			string source
		) {
			var document = CreateDocument( source );

			var diagnostic = GetSortedDiagnosticsFromDocuments(
				GetCSharpDiagnosticAnalyzer(),
				new[] { document }
			).FirstOrDefault();

			// Nothing left to fix
			if( diagnostic == null ) {
				return source;
			}

			var patchedDocument = GetPatchedDocument(
				document,
				diagnostic,
				GetCSharpCodeFixProvider()
			);

			var updatedSyntaxTree = patchedDocument.GetSyntaxTreeAsync().Result;
			var updatedSource = updatedSyntaxTree.ToString();

			// Keep applying fixes until document is free of diagnostics
			return ApplyCodeFixesRecursive( updatedSource );
		}

		private Document GetPatchedDocument(
			Document document,
			Diagnostic diagnostic,
			CodeFixProvider codeFixer
		) {
			var diagnosticActions = new List<CodeAction>();
			var codeActionRegistration = new Action<CodeAction, ImmutableArray<Diagnostic>>(
				( codeAction, applicationDiagnostics ) => {
					if( applicationDiagnostics.Contains( diagnostic ) ) {
						diagnosticActions.Add( codeAction );
					}
				}
			);

			var codeFixContext = new CodeFixContext(
				document,
				diagnostic,
				codeActionRegistration,
				new CancellationToken( false )
			);

			codeFixer.RegisterCodeFixesAsync( codeFixContext ).Wait();

			var action = diagnosticActions.FirstOrDefault();
			if( action == null ) {
				throw new Exception( $"No action available to fix diagnostic '{diagnostic.Id}'" );
			}

			var operation = action
				.GetOperationsAsync( new CancellationToken( false ) )
				.Result
				.FirstOrDefault() as ApplyChangesOperation;

			if( operation == null ) {
				throw new Exception(
					$"Failed to find applicable change operation for action '{action.Title}'"
				);
			}

			var updatedDocument = operation.ChangedSolution.GetDocument( document.Id );
			return updatedDocument;
		}
	}
}
