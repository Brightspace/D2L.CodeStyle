using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static D2L.CodeStyle.Analyzers.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DataRecordConverters {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class InterfaceBinderAnalyzer : DiagnosticAnalyzer {

		private const string InterfaceBinderFullName = "D2L.LP.LayeredArch.Data.InterfaceBinder`1";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			InterfaceBinder_InterfacesOnly
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private void CompilationStart( CompilationStartAnalysisContext context ) {

			Compilation comp = context.Compilation;
			if( !comp.TryGetTypeByMetadataName( InterfaceBinderFullName, out INamedTypeSymbol? interfaceBinderType ) ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeGenericName( c, interfaceBinderType, (GenericNameSyntax)c.Node ),
					SyntaxKind.GenericName
				);
		}

		private static void AnalyzeGenericName(
				SyntaxNodeAnalysisContext context,
				INamedTypeSymbol interfaceBinderType,
				GenericNameSyntax genericName
			) {

			TypeArgumentListSyntax argumentList = genericName.TypeArgumentList;
			if( argumentList.Arguments.Count != 1 ) {
				return;
			}

			ITypeSymbol? genericType = context.SemanticModel
				.GetTypeInfo( genericName, context.CancellationToken )
				.Type;

			if( genericType == null ) {
				return;
			}

			if( !SymbolEqualityComparer.Default.Equals( genericType.OriginalDefinition, interfaceBinderType ) ) {
				return;
			}

			TypeSyntax interfaceArgument = argumentList.Arguments[ 0 ];

			ITypeSymbol? interfaceType = context.SemanticModel
				.GetTypeInfo( interfaceArgument, context.CancellationToken )
				.Type;

			if( interfaceType == null ) {
				return;
			}

			if( interfaceType.TypeKind == TypeKind.Interface ) {
				return;
			}

			context.ReportDiagnostic(
					InterfaceBinder_InterfacesOnly,
					interfaceArgument.GetLocation(),
					messageArgs: new[] { interfaceType.Name }
				);
		}
	}
}
