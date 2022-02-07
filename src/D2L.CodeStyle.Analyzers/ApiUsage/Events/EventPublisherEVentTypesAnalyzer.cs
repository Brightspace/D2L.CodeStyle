using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Events {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class EventPublisherEventTypesAnalyzer : DiagnosticAnalyzer {

		private const string EventAttributeFullName = "D2L.LP.Distributed.Events.Domain.EventAttribute";

		private static readonly ImmutableArray<string> PublisherTypeNames = ImmutableArray.Create(
			"D2L.LP.Distributed.Events.Domain.IEventNotifier",
			"D2L.LP.Distributed.Events.Domain.IEventPublisher",
			"D2L.LP.Distributed.Events.Processing.Domain.RefiredEventEnvelope"
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.EventTypeMissingEventAttribute
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol? eventAttributeType = compilation.GetTypeByMetadataName( EventAttributeFullName );
			if( eventAttributeType.IsNullOrErrorType() ) {
				return;
			}

			IImmutableSet<ISymbol> genericPublishMethods = PublisherTypeNames
				.SelectMany( typeName => GetGenericPublishMethods( compilation, typeName ) )
				.ToImmutableHashSet( SymbolEqualityComparer.Default );

			context.RegisterOperationAction(
					ctxt => AnalyzeMethodInvocation(
						ctxt,
						(IInvocationOperation)ctxt.Operation,
						eventAttributeType,
						genericPublishMethods
					),
					OperationKind.Invocation
				);
		}

		private void AnalyzeMethodInvocation(
				OperationAnalysisContext context,
				IInvocationOperation invocation,
				INamedTypeSymbol eventAttributeType,
				IImmutableSet<ISymbol> genericPublishMethods
			) {

			IMethodSymbol methodSymbol = invocation.TargetMethod;
			if( !methodSymbol.IsGenericMethod ) {
				return;
			}

			if( !genericPublishMethods.Contains( methodSymbol.OriginalDefinition ) ) {
				return;
			}

			ITypeSymbol eventTypeSymbol = methodSymbol.TypeArguments[ 0 ];
			if( eventTypeSymbol.IsNullOrErrorType() ) {
				return;
			}

			bool hasEventAttr = eventTypeSymbol
				.GetAttributes()
				.Any( attr => SymbolEqualityComparer.Default.Equals( attr.AttributeClass, eventAttributeType ) );

			if( hasEventAttr ) {
				return;
			}

			if( eventTypeSymbol.TypeKind == TypeKind.TypeParameter ) {
				return;
			}

			context.ReportDiagnostic(
					Diagnostics.EventTypeMissingEventAttribute,
					invocation.Syntax.GetLocation(),
					messageArgs: new[] { eventTypeSymbol.ToDisplayString() }
				);
		}

		private static IEnumerable<ISymbol> GetGenericPublishMethods(
				Compilation compilation,
				string publisherTypeName
			) {

			INamedTypeSymbol? publisherType = compilation.GetTypeByMetadataName( publisherTypeName );
			if( publisherType.IsNullOrErrorType() ) {
				return Enumerable.Empty<ISymbol>();
			}

			IEnumerable<IMethodSymbol> methods = publisherType
				.GetMembers()
				.OfType<IMethodSymbol>()
				.Where( m => (
					m.IsGenericMethod
					&& m.TypeArguments.Length == 1
				) );

			return methods;
		}
	}
}
