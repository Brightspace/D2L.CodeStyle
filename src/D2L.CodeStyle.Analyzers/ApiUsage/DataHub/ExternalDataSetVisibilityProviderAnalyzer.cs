using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DataHub {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal class ExternalDataSetVisibilityProviderAnalyzer : DiagnosticAnalyzer {

		public const string IEventDrivenDataSetPluginFullName = "D2L.AW.DataExport.BrightspaceDataSets.Domain.IEventDrivenDataSetPlugin";
		public const string ExternalDataSetVisibilityProviderFullName = "D2L.AW.DataExport.BrightspaceDataSets.Domain.IExternalDataSetVisibilityProvider`1";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.ExternalDataSetVisibilityProviderTypeParameterMatchesClass
			);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol IEventDrivenDataSetPluginType = compilation.GetTypeByMetadataName( IEventDrivenDataSetPluginFullName );
			if( IEventDrivenDataSetPluginType.IsNullOrErrorType() ) {
				return;
			}

			INamedTypeSymbol ExternalDataSetVisibilityProviderType = compilation.GetTypeByMetadataName( ExternalDataSetVisibilityProviderFullName );
			if( ExternalDataSetVisibilityProviderType.IsNullOrErrorType() ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeClassDeclaration( c, IEventDrivenDataSetPluginType, ExternalDataSetVisibilityProviderType ),
					SyntaxKind.ClassDeclaration
				);
		}

		private void AnalyzeClassDeclaration(
				SyntaxNodeAnalysisContext context,
				INamedTypeSymbol IEventDrivenDataSetPluginType,
				INamedTypeSymbol ExternalDataSetVisibilityProviderType
			) {

			ClassDeclarationSyntax classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

			INamedTypeSymbol baseSymbol = context.SemanticModel.GetDeclaredSymbol( classDeclarationSyntax );
			if( !baseSymbol.Interfaces.Contains( IEventDrivenDataSetPluginType ) ) {
				return;
			}

			IMethodSymbol constuctor = baseSymbol.Constructors.Single();

			IParameterSymbol foundParameterSymbol = constuctor.Parameters.FirstOrDefault( p => {
				INamedTypeSymbol parameter = p.Type as INamedTypeSymbol;

				if( parameter == null ) {
					return false;
				}

				return parameter.IsGenericType && parameter.ConstructedFrom == ExternalDataSetVisibilityProviderType;
			} );

			// After split of `IEventDrivenDataSetPlugin` to `IEventDrivenDataSetPlugin` and `IExternalDataSetPlugin`,
			// we probably want to ensure that we inject `IExternalDataSetVisibilityProvider` all the time for
			// `IExternalDataSetPlugin` implementations
			if( foundParameterSymbol == null ) {
				return;
			}

			ITypeSymbol expectedType = ExternalDataSetVisibilityProviderType.Construct( baseSymbol );

			if( foundParameterSymbol.Type == expectedType ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
				Diagnostics.ExternalDataSetVisibilityProviderTypeParameterMatchesClass,
				foundParameterSymbol.GetDeclarationSyntax<ParameterSyntax>().GetLocation(),
				ExternalDataSetVisibilityProviderType.Name,
				baseSymbol.Name
			);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
