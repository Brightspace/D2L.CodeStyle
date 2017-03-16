using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.IGlobalContext {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class IGlobalContextAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "D2L0012";
		private const string Category = "Safety";

		private const string Title = "Stop using 'IGlobalContext'.";
		private const string Description = "'IGlobalContext' made a lot of sense before dependency injection, but now you can and should just inject the things you need.";
		internal const string MessageFormat = "Stop using 'IGlobalContext', find what you should use instead: http://cdo-blog.s3-website-us-east-1.amazonaws.com/2017/03/15/Stop-using-IGlobalContext/ ";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

		public override void Initialize( AnalysisContext context ) {
			context.RegisterCompilationStartAction( ctx => {
				ctx.RegisterSyntaxNodeAction( AnalyzeField, SyntaxKind.FieldDeclaration );
				ctx.RegisterSyntaxNodeAction( AnalyzeProperty, SyntaxKind.PropertyDeclaration );
				ctx.RegisterSyntaxNodeAction( AnalyzeMethod, SyntaxKind.MethodDeclaration );
			} );
		}

		private void AnalyzeField( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as FieldDeclarationSyntax;
			if( root == null ) {
				return;
			}

			if( root.Declaration.Type.ToString().Equals( "IGlobalContext" ) ) {
				var diagnostic = Diagnostic.Create( Rule, root.Declaration.Type.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			};
		}

		private void AnalyzeProperty( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as PropertyDeclarationSyntax;
			if( root == null ) {
				return;
			}

			if( root.Type.ToString().Equals( "IGlobalContext" ) ) {
				var diagnostic = Diagnostic.Create( Rule, root.Type.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}
		}

		private void AnalyzeMethod( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as MethodDeclarationSyntax;
			if( root == null ) {
				return;
			}

			if( root.ReturnType.ToString().Equals( "IGlobalContext" ) ) {
				var diagnostic = Diagnostic.Create( Rule, root.ReturnType.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}

			if( root.ParameterList != null ) {
				var parameters = root.ParameterList.Parameters.ToImmutableArray();
				foreach( var parameter in parameters ) {
					if( parameter.Type.ToString().Equals( "IGlobalContext" ) ) {
						var diagnostic = Diagnostic.Create( Rule, parameter.Type.GetLocation() );
						context.ReportDiagnostic( diagnostic );
					}
				}
			}
		}
	}
}
