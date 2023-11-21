#nullable enable

using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterScopeBuilderAnalyzer );
		}

		private void RegisterScopeBuilderAnalyzer( CompilationStartAnalysisContext context ) {

			Compilation comp = context.Compilation;
			if( !comp.TryGetTypeByMetadataName( SerializerAttributeFullName, out INamedTypeSymbol? serializerAttributeType ) ) {
				return;
			}

			var serializerInterfaceTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
			if( comp.TryGetTypeByMetadataName( ISerializerFullName, out INamedTypeSymbol? serializerType ) ) {
				serializerInterfaceTypes.Add( serializerType );
			}
			if( comp.TryGetTypeByMetadataName( ITrySerializerFullName, out INamedTypeSymbol? trySerializerType ) ) {
				serializerInterfaceTypes.Add( trySerializerType );
			}

			context.RegisterOperationAction(
				ctx => AnalyzeAttribute(
					ctx,
					(IAttributeOperation)ctx.Operation,
					serializerAttributeType,
					serializerInterfaceTypes.ToImmutable()
				),
				OperationKind.Attribute
			);
		}

		private static void AnalyzeAttribute(
				OperationAnalysisContext context,
				IAttributeOperation attribute,
				INamedTypeSymbol serializerAttributeType,
				ImmutableArray<INamedTypeSymbol> serializerInterfaceTypes
			) {

			// Some sort of error if Operation isn't an ObjectCreation (alternatively an IInvalidOperation)
			if( attribute.Operation is not IObjectCreationOperation attributeCreation ) {
				return;
			}

			if( !SymbolEqualityComparer.Default.Equals( attributeCreation.Type, serializerAttributeType ) ) {
				return;
			}

			if( attributeCreation.Arguments.IsEmpty ) {
				return;
			}

			IArgumentOperation argument = attributeCreation.Arguments[ 0 ];

			if( argument.Value is not ITypeOfOperation typeofOperation ) {
				ReportInvalidSerializerType(
						context,
						argument.Value.Syntax,
						messageArg: argument.Value.Syntax.ToString()
					);
				return;
			}

			if( DoesImplementOneOfSerializerInterfaces(
					typeofOperation.TypeOperand,
					serializerInterfaceTypes
				) ) {
				return;
			}

			ReportInvalidSerializerType(
					context,
					typeofOperation.Syntax,
					messageArg: typeofOperation.TypeOperand.ToDisplayString( TypeDisplayFormat )
				);
		}

		private static void ReportInvalidSerializerType(
				OperationAnalysisContext context,
				SyntaxNode typeArgumentSyntax,
				string messageArg
			) {

			context.ReportDiagnostic(
					descriptor: Diagnostics.InvalidSerializerType,
					location: typeArgumentSyntax.GetLocation(),
					messageArgs: new[] { messageArg }
				);
		}

		private static bool DoesImplementOneOfSerializerInterfaces(
				ITypeSymbol serializerType,
				ImmutableArray<INamedTypeSymbol> serializerInterfaceTypes
			) {

			ImmutableArray<INamedTypeSymbol> interfaces;

			if( serializerType.IsDefinition ) {
				interfaces = serializerType.AllInterfaces;
			} else {
				interfaces = serializerType.OriginalDefinition.AllInterfaces;
			}

			return interfaces.Any( serializerInterfaceTypes.Contains );
		}
	}
}
