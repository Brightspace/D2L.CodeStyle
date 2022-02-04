using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class OldAndBrokenServiceLocatorAnalyzer : DiagnosticAnalyzer {

		private static readonly ImmutableArray<string> DisallowedTypeMetadataNames = ImmutableArray.Create(
				"D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocator",
				"D2L.LP.Extensibility.Activation.Domain.OldAndBrokenServiceLocatorFactory",
				"D2L.LP.Extensibility.Activation.Domain.IObjectActivator",
				"D2L.LP.Extensibility.Activation.Domain.ICustomObjectActivator",
				"D2L.LP.Extensibility.Activation.Domain.ObjectActivatorExtensions",
				"D2L.LP.Extensibility.Activation.Domain.ObjectActivatorFactory",
				"D2L.LP.Extensibility.Activation.Domain.Default.StaticDI.StaticDILocator"
			);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.OldAndBrokenLocatorIsObsolete,
			Diagnostics.UnnecessaryAllowedListEntry
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterServiceLocatorAnalyzer );
		}

		public void RegisterServiceLocatorAnalyzer( CompilationStartAnalysisContext context ) {

			ImmutableHashSet<ITypeSymbol> disallowedTypes = GetDisallowedTypes( context.Compilation );
			if( disallowedTypes.IsEmpty ) {
				return;
			}

			AllowedTypeList allowedTypeList = AllowedTypeList.CreateFromAnalyzerOptions(
				allowedListFileName: "OldAndBrokenServiceLocatorAllowedList.txt",
				analyzerOptions: context.Options
			);

			TypeRuleSets typeRules = new( allowedTypeList, disallowedTypes );

			context.RegisterOperationAction(
				context => {
					IInvocationOperation invocation = (IInvocationOperation)context.Operation;
					AnalyzeMemberUsage( context, invocation.TargetMethod, typeRules );
				},
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				context => {
					IMethodReferenceOperation reference = (IMethodReferenceOperation)context.Operation;
					AnalyzeMemberUsage( context, reference.Method, typeRules );
				},
				OperationKind.MethodReference
			);

			context.RegisterOperationAction(
				context => {
					IPropertyReferenceOperation reference = (IPropertyReferenceOperation)context.Operation;
					AnalyzeMemberUsage( context, reference.Property, typeRules );
				},
				OperationKind.PropertyReference
			);

			context.RegisterSymbolAction(
				context => {
					IFieldSymbol field = (IFieldSymbol)context.Symbol;
					AnalyzeTypeUsage( context, field.Type, typeRules );
				},
				SymbolKind.Field
			);

			context.RegisterSymbolAction(
				context => {
					IParameterSymbol parameter = (IParameterSymbol)context.Symbol;
					AnalyzeTypeUsage( context, parameter.Type, typeRules );
				},
				SymbolKind.Parameter
			);

			context.RegisterSymbolAction(
				context => {
					IPropertySymbol property = (IPropertySymbol)context.Symbol;
					AnalyzeTypeUsage( context, property.Type, typeRules );
				},
				SymbolKind.Property
			);

			context.RegisterSymbolAction(
				allowedTypeList.CollectSymbolIfContained,
				SymbolKind.NamedType
			);

			context.RegisterCompilationEndAction(
				allowedTypeList.ReportUnnecessaryEntries
			);
		}

		private void AnalyzeMemberUsage(
			OperationAnalysisContext context,
			ISymbol member,
			TypeRuleSets typeRules
		) {

			if( !typeRules.Disallowed.Contains( member.ContainingType ) ) {
				return;
			}

			if( HasExemption( context.ContainingSymbol, typeRules ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostics.OldAndBrokenLocatorIsObsolete,
				context.Operation.Syntax.GetLocation()
			);
		}

		private void AnalyzeTypeUsage(
			SymbolAnalysisContext context,
			ITypeSymbol type,
			TypeRuleSets typeRules
		) {

			if( !typeRules.Disallowed.Contains( type ) ) {
				return;
			}

			if( HasExemption( context.Symbol, typeRules ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostics.OldAndBrokenLocatorIsObsolete,
				context.Symbol.Locations[ 0 ]
			);
		}

		private bool HasExemption(
				ISymbol symbol,
				TypeRuleSets typeRules
			) {

			ImmutableArray<INamedTypeSymbol> containingTypes = symbol.GetAllContainingTypes();

			// Allow the DI framework to call the disallowed types
			if( containingTypes.Any( Attributes.DIFramework.IsDefined ) ) {
				return true;
			}

			// Allow the types listed in OldAndBrokenServiceLocatorAllowedList.txt
			if( containingTypes.Any( typeRules.Allowed.Contains ) ) {
				return true;
			}

			return false;
		}

		private ImmutableHashSet<ITypeSymbol> GetDisallowedTypes( Compilation compilation ) {

			var builder = ImmutableHashSet.CreateBuilder<ITypeSymbol>( SymbolEqualityComparer.Default );

			foreach( string metadataName in DisallowedTypeMetadataNames ) {

				INamedTypeSymbol? factoryType = compilation.GetTypeByMetadataName( metadataName );
				if( !factoryType.IsNullOrErrorType() ) {
					builder.Add( factoryType );
				}
			}

			return builder.ToImmutable();
		}

		private readonly record struct TypeRuleSets(
			AllowedTypeList Allowed,
			ImmutableHashSet<ITypeSymbol> Disallowed
		);
	}
}
