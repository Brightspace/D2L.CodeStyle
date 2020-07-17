using System.Collections.Generic;
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
		private const string ISerializerFullName = "D2L.LP.Serialization.ISerializer";
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

			var serializerInterfaceTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
			if( comp.TryGetTypeByMetadataName( ISerializerFullName, out INamedTypeSymbol serializerType ) ) {
				serializerInterfaceTypes.Add( serializerType );
			}
			if( comp.TryGetTypeByMetadataName( ITrySerializerFullName, out INamedTypeSymbol trySerializerType ) ) {
				serializerInterfaceTypes.Add( trySerializerType );
			}

			context.RegisterSyntaxNodeAction(
					c => AnalyzeAttributeSyntax(
						c,
						(AttributeSyntax)c.Node,
						serializerAttributeType,
						serializerInterfaceTypes.ToImmutable()
					),
					SyntaxKind.Attribute
				);
		}

		private void AnalyzeAttributeSyntax(
				SyntaxNodeAnalysisContext context,
				AttributeSyntax attributeSyntax,
				INamedTypeSymbol serializerAttributeType,
				ImmutableArray<INamedTypeSymbol> serializerInterfaceTypes
			) {

			SemanticModel model = context.SemanticModel;
			if( !model.IsAttributeOfType( attributeSyntax, serializerAttributeType ) ) {
				return;
			}

			AttributeArgumentListSyntax argumentList = attributeSyntax.ArgumentList;
			if( argumentList == null ) {
				return;
			}

			SeparatedSyntaxList<AttributeArgumentSyntax> arguments = argumentList.Arguments;
			if( arguments.Count == 0 ) {
				return;
			}

			AttributeArgumentSyntax typeArgumentSyntax = arguments[ 0 ];
			if( !( typeArgumentSyntax.Expression is TypeOfExpressionSyntax typeofSyntax ) ) {

				ReportInvalidSerializerType(
						context,
						typeArgumentSyntax,
						messageArg: typeArgumentSyntax.Expression.ToString()
					);
				return;
			}

			ITypeSymbol serializerType = model.GetTypeInfo( typeofSyntax.Type ).Type;

			bool isSerializerType = serializerType.AllInterfaces.Any( serializerInterfaceTypes.Contains );
			if( isSerializerType ) {
				return;
			}

			ReportInvalidSerializerType(
					context,
					typeArgumentSyntax,
					messageArg: serializerType.ToDisplayString( TypeDisplayFormat )
				);
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
