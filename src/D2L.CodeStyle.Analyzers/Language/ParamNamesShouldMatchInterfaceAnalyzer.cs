using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ParamNamesShouldMatchInterfaceAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.InterfaceImplementationParamNameMismatch
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer( CompilationStartAnalysisContext context ) {
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeClassDefinition(
					ctx,
					( INamedTypeSymbol )ctx.SemanticModel.GetDeclaredSymbol( ctx.Node )
				),
				SyntaxKind.ClassDeclaration
			);
		}

		private static void AnalyzeClassDefinition(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol typeSymbol
		) {
			if( !typeSymbol.Interfaces.Any() ) {
				return;
			}

			ImmutableArray<IMethodSymbol> interfaceMethods = CollectInterfaceMethods( typeSymbol );
			foreach( var interfaceMethod in interfaceMethods ) {
				IMethodSymbol implMethod = (IMethodSymbol)typeSymbol.FindImplementationForInterfaceMember( interfaceMethod );

				// In the middle of coding and haven't implemented an interface method yet
				if( implMethod == null ) {
					continue;
				}

				// Nothing to do
				if( implMethod.Parameters.Length == 0 ) {
					continue;
				}

				ImmutableArray<IParameterSymbol> implParameters = implMethod.Parameters;
				ImmutableArray<IParameterSymbol> interfaceParameters = interfaceMethod.Parameters;
				for( int i = 0; i < implParameters.Length; ++i ) {
					IParameterSymbol implParameter = implParameters[ i ];
					IParameterSymbol interfaceParameter = interfaceParameters[ i ];

					if( implParameter.Name.Equals( interfaceParameter.Name, StringComparison.InvariantCultureIgnoreCase ) ) {
						continue;
					}

					// don't know when this happens
					if( implParameter.DeclaringSyntaxReferences.Length == 0 ) {
						continue;
					}

					Location location = implParameter.DeclaringSyntaxReferences.First().GetSyntax().GetLocation();
					context.ReportDiagnostic( Diagnostic.Create(
						Diagnostics.InterfaceImplementationParamNameMismatch,
						location,
						interfaceParameter.Name,
						implParameter.Name
					) );
				}
			}
		}

		private static ImmutableArray<IMethodSymbol> CollectInterfaceMethods(
			INamedTypeSymbol typeSymbol
		) {
			return typeSymbol
				.AllInterfaces
				.SelectMany( @interface => @interface
					.GetMembers()
					.OfType<IMethodSymbol>()
					.Where( m => m.MethodKind == MethodKind.Ordinary )
				)
				.ToImmutableArray();
		}
	}
}
