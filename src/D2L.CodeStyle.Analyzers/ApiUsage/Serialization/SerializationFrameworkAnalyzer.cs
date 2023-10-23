using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Serialization {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class SerializationFrameworkAnalyzer : DiagnosticAnalyzer {

		private static readonly ImmutableArray<string> DisallowedTypeMetadataNames = ImmutableArray.Create(
			"D2L.LP.Serialization.ISerializationWriter",
			"D2L.LP.Serialization.ISerializer",
			"D2L.LP.Serialization.ISerializationWriterExtensions",
			"D2L.LP.Serialization.ITrySerializer"
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.DangerousSerializationTypeReference
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		public static void RegisterAnalyzer( CompilationStartAnalysisContext context ) {

			ImmutableHashSet<ITypeSymbol> dangerousInterfaces = GetDangerousInterfaces( context.Compilation );

			if( dangerousInterfaces.IsEmpty ) {
				return;
			}

			context.RegisterOperationAction(
				context => {
					IInvocationOperation invocation = (IInvocationOperation)context.Operation;
					AnalyzeMemberUsage( context, invocation.TargetMethod, dangerousInterfaces );
				},
				OperationKind.Invocation
			);

			context.RegisterOperationAction(
				context => {
					IMethodReferenceOperation reference = (IMethodReferenceOperation)context.Operation;
					AnalyzeMemberUsage( context, reference.Method, dangerousInterfaces );
				},
				OperationKind.MethodReference
			);

			context.RegisterOperationAction(
				context => {
					IPropertyReferenceOperation reference = (IPropertyReferenceOperation)context.Operation;
					AnalyzeMemberUsage( context, reference.Property, dangerousInterfaces );
				},
				OperationKind.PropertyReference
			);

			context.RegisterSymbolAction(
				context => {
					IFieldSymbol field = (IFieldSymbol)context.Symbol;
					AnalyzeTypeUsage( context, field.Type, dangerousInterfaces );
				},
				SymbolKind.Field
			);

			context.RegisterSymbolAction(
				context => {
					IPropertySymbol property = (IPropertySymbol)context.Symbol;
					AnalyzeTypeUsage( context, property.Type, dangerousInterfaces );
				},
				SymbolKind.Property
			);
		}

		private static void AnalyzeMemberUsage(
			OperationAnalysisContext context,
			ISymbol member,
			ImmutableHashSet<ITypeSymbol> bannedTypes
		) {
			if( !ImplementsDangerousInterface( bannedTypes, member.ContainingType ) ) {
				return;
			}

			if( IsSerializationFrameworkInternal( context.ContainingSymbol ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostics.DangerousSerializationTypeReference,
				context.Operation.Syntax.GetLocation()
			);
		}

		private static void AnalyzeTypeUsage(
			SymbolAnalysisContext context,
			ITypeSymbol type,
			ImmutableHashSet<ITypeSymbol> bannedTypes
		) {
			if( !ImplementsDangerousInterface( bannedTypes, type ) ) {
				return;
			}

			if( IsSerializationFrameworkInternal( context.Symbol ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostics.DangerousSerializationTypeReference,
				context.Symbol.Locations[0]
			);
		}

		private static bool ImplementsDangerousInterface( ImmutableHashSet<ITypeSymbol> dangerousInterface, ITypeSymbol type ) =>
			dangerousInterface.Contains( type ) || type.AllInterfaces.Any( dangerousInterface.Contains );

		private static bool IsSerializationFrameworkInternal( ISymbol symbol ) =>
			symbol.GetAllContainingTypes().Any( Attributes.SerializationFramework.IsDefined );

		private static ImmutableHashSet<ITypeSymbol> GetDangerousInterfaces( Compilation compilation ) {
			ImmutableHashSet<ITypeSymbol> bannedTypes = DisallowedTypeMetadataNames
				.Select( compilation.GetTypeByMetadataName )
				.Where( t => !t.IsNullOrErrorType() )
				.OfType<ITypeSymbol>()
				.ToImmutableHashSet();

			return bannedTypes;
		}

	}
}
