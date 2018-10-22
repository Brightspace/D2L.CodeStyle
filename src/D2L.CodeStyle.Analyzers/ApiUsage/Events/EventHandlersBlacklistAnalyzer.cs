using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Events {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class EventHandlersBlacklistAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.EventHandlerBlacklisted
			);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			IImmutableSet<INamedTypeSymbol> blacklistedTypes = GetBlacklistedTypes( compilation );
			if( blacklistedTypes.Count == 0 ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeSimpleBaseType( c, blacklistedTypes ),
					SyntaxKind.SimpleBaseType
				);
		}

		private void AnalyzeSimpleBaseType(
				SyntaxNodeAnalysisContext context,
				IImmutableSet<INamedTypeSymbol> blacklistedTypes
			) {

			SimpleBaseTypeSyntax baseTypeSyntax = (SimpleBaseTypeSyntax)context.Node;
			SymbolInfo baseTypeSymbol = context.SemanticModel.GetSymbolInfo( baseTypeSyntax.Type );

			INamedTypeSymbol baseSymbol = ( baseTypeSymbol.Symbol as INamedTypeSymbol );
			if( baseSymbol.IsNullOrErrorType() ) {
				return;
			}

			if( !blacklistedTypes.Contains( baseSymbol ) ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.EventHandlerBlacklisted,
					baseTypeSyntax.GetLocation(),
					baseSymbol.ToDisplayString()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static IImmutableSet<INamedTypeSymbol> GetBlacklistedTypes( Compilation compilation ) {

			IImmutableSet<INamedTypeSymbol> types = EventHandlersBlacklist.BlacklistedTypes
				.SelectMany( genericType => GetGenericTypes( compilation, genericType.Key, genericType.Value ) )
				.ToImmutableHashSet();

			return types;
		}

		private static IEnumerable<INamedTypeSymbol> GetGenericTypes(
				Compilation compilation,
				string genericTypeName,
				ImmutableArray<ImmutableArray<string>> genericTypeArgumentSets
			) {

			INamedTypeSymbol genericTypeDefinition = compilation.GetTypeByMetadataName( genericTypeName );
			if( genericTypeDefinition.IsNullOrErrorType() ) {
				yield break;
			}

			foreach( ImmutableArray<string> genericTypeArguments in genericTypeArgumentSets ) {
				if( TryGetGenericType( compilation, genericTypeDefinition, genericTypeArguments, out INamedTypeSymbol genericType ) ) {
					yield return genericType;
				}
			}
		}

		private static bool TryGetGenericType(
				Compilation compilation,
				INamedTypeSymbol genericTypeDefinition,
				ImmutableArray<string> genericTypeArguments,
				out INamedTypeSymbol genericType
			) {

			INamedTypeSymbol[] genericTypeArgumentSymbols = new INamedTypeSymbol[ genericTypeArguments.Length ];

			for( int i = 0; i < genericTypeArguments.Length; i++ ) {
				string genericTypeArgumentName = genericTypeArguments[ i ];

				INamedTypeSymbol genericTypeArgumentSymbol = compilation.GetTypeByMetadataName( genericTypeArgumentName );
				if( genericTypeArgumentSymbol.IsNullOrErrorType() ) {

					genericType = null;
					return false;
				}

				genericTypeArgumentSymbols[ i ] = genericTypeArgumentSymbol;
			}

			genericType = genericTypeDefinition.Construct( genericTypeArgumentSymbols );
			if( genericType.IsNullOrErrorType() ) {

				genericType = null;
				return false;
			}

			return true;
		}
	}
}
