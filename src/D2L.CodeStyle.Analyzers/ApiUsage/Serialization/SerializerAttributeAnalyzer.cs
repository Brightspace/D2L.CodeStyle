using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class SerializerAttributeAnalyzer : DiagnosticAnalyzer {

		private static readonly SymbolDisplayFormat TypeDisplayFormat = new SymbolDisplayFormat(
				memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
			);

		private const string SerializerAttributeFullName = "D2L.LP.Serialization.SerializerAttribute";
		private const string ITrySerializerFullName = "D2L.LP.Serialization.ITrySerializer";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.InvalidSerializerType
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterScopeBuilderAnalyzer );
		}

		private void RegisterScopeBuilderAnalyzer( CompilationStartAnalysisContext context ) {

			Compilation comp = context.Compilation;
			if( !comp.TryGetTypeByMetadataName( SerializerAttributeFullName, out INamedTypeSymbol serializerAttributeType ) ) {
				return;
			}
			if( !comp.TryGetTypeByMetadataName( ITrySerializerFullName, out INamedTypeSymbol trySerializerType ) ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttributeSyntax(
						c,
						(AttributeSyntax)c.Node,
						serializerAttributeType,
						trySerializerType
					),
					SyntaxKind.Attribute
				);
		}

		private void AnalyzeAttributeSyntax(
				SyntaxNodeAnalysisContext context,
				AttributeSyntax attributeSyntax,
				INamedTypeSymbol serializerAttributeType,
				INamedTypeSymbol trySerializerType
			) {

			SemanticModel model = context.SemanticModel;
			if( !IsAttributeOfType( model, attributeSyntax, serializerAttributeType ) ) {
				return;
			}


			AttributeArgumentSyntax typeArgumentSyntax = attributeSyntax.ArgumentList.Arguments[ 0 ];
			if( !( typeArgumentSyntax.Expression is TypeOfExpressionSyntax typeofSyntax ) ) {

				ReportInvalidSerializerType(
						context,
						typeArgumentSyntax,
						messageArg: typeArgumentSyntax.Expression.ToString()
					);
				return;
			}

			ITypeSymbol serializerType = model.GetTypeInfo( typeofSyntax.Type ).Type;
			if( serializerType.AllInterfaces.Contains( trySerializerType ) ) {
				return;
			}
			
			ReportInvalidSerializerType(
					context,
					typeArgumentSyntax,
					messageArg: serializerType.ToDisplayString( TypeDisplayFormat )
				);
		}

		private static bool IsAttributeOfType(
				SemanticModel model,
				AttributeSyntax attributeSyntax,
				INamedTypeSymbol attributeType
			) {

			TypeInfo typeInfo = model.GetTypeInfo( attributeSyntax );
			if( !( typeInfo.Type is INamedTypeSymbol namedSymbol ) ) {
				return false;
			}

			if( !namedSymbol.Equals( attributeType ) ) {
				return false;
			}

			return true;
		}

		private void ReportInvalidSerializerType(
				SyntaxNodeAnalysisContext context,
				AttributeArgumentSyntax typeArgumentSyntax,
				string messageArg
			) {

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: Diagnostics.InvalidSerializerType,
					location: typeArgumentSyntax.GetLocation(),
					messageArgs: messageArg
				);

			context.ReportDiagnostic( diagnostic );
		}
	}
}
