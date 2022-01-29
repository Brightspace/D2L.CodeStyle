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

			INamedTypeSymbol eventAttributeType = compilation.GetTypeByMetadataName( EventAttributeFullName );
			if( eventAttributeType.IsNullOrErrorType() ) {
				return;
			}

			IImmutableSet<ISymbol> genericPublishMethods = PublisherTypeNames
				.SelectMany( typeName =>  GetGenericPublishMethods( compilation, typeName ) )
				.ToImmutableHashSet( SymbolEqualityComparer.Default );

			context.RegisterSyntaxNodeAction(
					ctxt => AnalyzeMethodInvocation(
						ctxt,
						(InvocationExpressionSyntax)ctxt.Node,
						eventAttributeType,
						genericPublishMethods
					),
					SyntaxKind.InvocationExpression
				);
		}

		private void AnalyzeMethodInvocation(
				SyntaxNodeAnalysisContext context,
				InvocationExpressionSyntax invocation,
				INamedTypeSymbol eventAttributeType,
				IImmutableSet<ISymbol> genericPublishMethods
			) {

			ISymbol expessionSymbol = context.SemanticModel
				.GetSymbolInfo( invocation.Expression, context.CancellationToken )
				.Symbol;

			if( expessionSymbol.IsNullOrErrorType() ) {
				return;
			}

			if( !( expessionSymbol is IMethodSymbol methodSymbol ) ) {
				return;
			}

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
				.Any( attr => attr.AttributeClass.Equals( eventAttributeType, SymbolEqualityComparer.Default ) );

			if( hasEventAttr ) {
				return;
			}

			if( eventTypeSymbol.TypeKind == TypeKind.TypeParameter ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.EventTypeMissingEventAttribute,
					invocation.GetLocation(),
					eventTypeSymbol.ToDisplayString()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static IEnumerable<ISymbol> GetGenericPublishMethods(
				Compilation compilation,
				string publisherTypeName
			) {

			INamedTypeSymbol publisherType = compilation.GetTypeByMetadataName( publisherTypeName );
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
