using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class AttributeAliasesAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.AliasingAttributeNamesNotSupported
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private void CompilationStart( CompilationStartAnalysisContext context ) {

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttribute( c, (AttributeSyntax)c.Node ),
					SyntaxKind.Attribute
				);
		}

		private void AnalyzeAttribute(
				SyntaxNodeAnalysisContext context,
				AttributeSyntax attribute
			) {

			// if it's not an identifier, then it's qualified and not aliasing the attribute name
			if( !( attribute.Name is IdentifierNameSyntax attributeName ) ) {
				return;
			}

			IEnumerable<IdentifierNameSyntax> usingAliases = GetUsingAliases( attribute );
			foreach( IdentifierNameSyntax usingAlias in usingAliases ) {

				if( !IsEquivalentToUsingAlias( attributeName, usingAlias ) ) {
					continue;
				}

				Diagnostic d = Diagnostic.Create(
						Diagnostics.AliasingAttributeNamesNotSupported,
						attribute.GetLocation()
					);

				context.ReportDiagnostic( d );
				return;
			}
		}

		private static bool IsEquivalentToUsingAlias(
				IdentifierNameSyntax attributeIdentifier,
				IdentifierNameSyntax usingAliasIdentifier
			) {

			string attributeName = attributeIdentifier.ToString();
			string usingAlias = usingAliasIdentifier.ToString();

			if( attributeName.Equals( usingAlias, StringComparison.Ordinal ) ) {
				return true;
			}

			const string attributeSuffix = "Attribute";
			if( !usingAlias.EndsWith( attributeSuffix, StringComparison.Ordinal ) ) {
				return false;
			}

			int shortUsingAliasLength = usingAlias.Length - attributeSuffix.Length;
			if( shortUsingAliasLength != attributeName.Length ) {
				return false;
			}

			bool equivalent = string.CompareOrdinal( attributeName, 0, usingAlias, 0, shortUsingAliasLength ) == 0;
			return equivalent;
		}

		private IEnumerable<IdentifierNameSyntax> GetUsingAliases( AttributeSyntax attribute ) {

			IEnumerable<UsingDirectiveSyntax> usingDirectives = attribute
				.Ancestors()
				.OfType<BaseNamespaceDeclarationSyntax>()
				.SelectMany( @namespace => @namespace.Usings );

			foreach( UsingDirectiveSyntax usingDirective in usingDirectives ) {

				NameEqualsSyntax? alias = usingDirective.Alias;
				if( alias == null ) {
					continue;
				}

				// ignore aliases that just import the class from the namespace
				SimpleNameSyntax unqualifiedUsingName = usingDirective.Name.GetUnqualifiedName();
				if( StringComparer.Ordinal.Equals( alias.Name.ToString(), unqualifiedUsingName.ToString() ) ) {
					continue;
				}

				yield return alias.Name;
			}
		}
	}
}
