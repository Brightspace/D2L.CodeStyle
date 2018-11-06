using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Events {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class EventTypesAnalyzer : DiagnosticAnalyzer {

		private const string EventAttributeFullName = "D2L.LP.Distributed.Events.Domain.EventAttribute";
		private const string ImmutableAttributeFullName = "D2L.CodeStyle.Annotations.Objects+ImmutableAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.EventTypeMissingImmutableAttribute
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			if( !compilation.TryGetTypeByMetadataName( EventAttributeFullName, out INamedTypeSymbol eventAttributeType ) ) {
				return;
			}

			INamedTypeSymbol immutableAttributeType = compilation.GetTypeByMetadataName( ImmutableAttributeFullName );

			context.RegisterSyntaxNodeAction(
					ctxt => AnalyzeMethodInvocation(
						ctxt,
						(ClassDeclarationSyntax)ctxt.Node,
						eventAttributeType,
						immutableAttributeType
					),
					SyntaxKind.ClassDeclaration
				);
		}

		private void AnalyzeMethodInvocation(
				SyntaxNodeAnalysisContext context,
				ClassDeclarationSyntax declaration,
				INamedTypeSymbol eventAttributeType,
				INamedTypeSymbol immutableAttributeType
			) {

			bool hasEventAttribute = HasAttribute( context, declaration, eventAttributeType );
			if( !hasEventAttribute ) {
				return;
			}

			bool hasImmutableAttirbute = HasAttribute( context, declaration, immutableAttributeType );
			if( hasImmutableAttirbute ) {
				return;
			}

			INamedTypeSymbol classSymbol = context.SemanticModel.GetDeclaredSymbol( declaration );

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.EventTypeMissingImmutableAttribute,
					declaration.Identifier.GetLocation(),
					classSymbol.ToDisplayString()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static bool HasAttribute(
				SyntaxNodeAnalysisContext context,
				ClassDeclarationSyntax declaration,
				INamedTypeSymbol attributeType
			) {

			if( attributeType == null ) {
				return false;
			}

			foreach( AttributeListSyntax attrList in declaration.AttributeLists ) {
				foreach( AttributeSyntax attr in attrList.Attributes ) {

					TypeInfo typeInfo = context.SemanticModel.GetTypeInfo( attr );

					ITypeSymbol type = typeInfo.Type;
					if( type.IsNullOrErrorType() ) {
						continue;
					}

					if( type.Equals( attributeType ) ) {
						return true;
					}
				}
			}

			return false;
		}
	}
}
