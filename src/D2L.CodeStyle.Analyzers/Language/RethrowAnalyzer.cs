using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed partial class RethrowAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.ShouldRethrow );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			context.RegisterOperationAction(
				Analyze,
				OperationKind.CatchClause
			);
		}

		private static void Analyze(
			OperationAnalysisContext context
		) {
			ICatchClauseOperation operation = context.Operation as ICatchClauseOperation;

			if( operation.ExceptionDeclarationOrExpression == null ) {
				// try {} catch {}
				// try {} catch( Exception ) {}
				return;
			}

			var throwOperations = operation.Handler.Operations.OfType<IThrowOperation>();
			if( !throwOperations.Any() ) {
				// try {} catch( Exception e ) { m_log.Error( e ); }
				return;
			}

			SyntaxToken capturedIdentifier = ( operation.ExceptionDeclarationOrExpression.Syntax as CatchDeclarationSyntax ).Identifier;

			SemanticModel model = context.Compilation.GetSemanticModel( operation.Syntax.SyntaxTree );
			DataFlowAnalysis dataFlow = model.AnalyzeDataFlow( operation.Handler.Syntax );
			if( dataFlow.WrittenInside.Any( s => s.Name == capturedIdentifier.ValueText ) ) {
				// try {} catch( Exception e ) { e = new Exception(); throw e; }
				return;
			}

			foreach( var throwOperation in throwOperations ) {
				if( throwOperation.Exception == null ) {
					// throw;
					continue;
				}

				if( !( throwOperation.Exception.Syntax is IdentifierNameSyntax thrownIdentifier ) ) {
					// throw new Exception();
					// throw MakeException();
					continue;
				}

				if( thrownIdentifier.Identifier.ValueText != capturedIdentifier.ValueText ) {
					continue;
				}

				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.ShouldRethrow,
					throwOperation.Syntax.GetLocation()
				) );
			}
		}
	}
}
