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
	internal sealed class EventHandlerLoaderTypesAnalyzer : DiagnosticAnalyzer {

		private const string EventAttributeFullName = "D2L.LP.Distributed.Events.Domain.EventAttribute";
		private const string EventHandlerAttributeFullName = "D2L.LP.Distributed.Events.Handlers.EventHandlerAttribute";

		private const string IEventHandlerRegistryFullName = "D2L.LP.Distributed.Events.Handlers.IEventHandlerRegistry";

		private static readonly IEnumerable<string> RegisterMethodNames = ImmutableArray.Create(
				"RegisterEventHandler",
				"RegisterOrgEventHandler"
			);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.EventTypeMissingEventAttribute,
			Diagnostics.EventHandlerTypeMissingEventAttribute
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol eventAttributeType = compilation.GetTypeByMetadataName( EventAttributeFullName );
			if( eventAttributeType.IsNullOrErrorType() ) {
				return;
			}

			INamedTypeSymbol eventHandlerAttributeType = compilation.GetTypeByMetadataName( EventHandlerAttributeFullName );
			if( eventHandlerAttributeType.IsNullOrErrorType() ) {
				return;
			}

			IImmutableSet<ISymbol> genericRegisterMethods = GetGenericRegisterMethods( compilation )
				.ToImmutableHashSet();

			context.RegisterSyntaxNodeAction(
					ctxt => AnalyzeMethodInvocation(
						ctxt,
						(InvocationExpressionSyntax)ctxt.Node,
						eventAttributeType,
						eventHandlerAttributeType,
						genericRegisterMethods
					),
					SyntaxKind.InvocationExpression
				);
		}

		private void AnalyzeMethodInvocation(
				SyntaxNodeAnalysisContext context,
				InvocationExpressionSyntax invocation,
				INamedTypeSymbol eventAttributeType,
				INamedTypeSymbol eventHandlerAttributeType,
				IImmutableSet<ISymbol> genericRegisterMethods
			) {

			ISymbol expessionSymbol = context.SemanticModel
				.GetSymbolInfo( invocation.Expression )
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

			if( !genericRegisterMethods.Contains( methodSymbol.OriginalDefinition ) ) {
				return;
			}

			ITypeSymbol eventTypeSymbol = methodSymbol.TypeArguments[ 0 ];
			if( eventTypeSymbol.IsNullOrErrorType() ) {
				return;
			}

			ITypeSymbol eventHandlerSymbol = methodSymbol.TypeArguments[ 1 ];
			if( eventHandlerSymbol.IsNullOrErrorType() ) {
				return;
			}

			InspectEventType( context, invocation, eventAttributeType, eventTypeSymbol );
			InspectEventHandlerType( context, invocation, eventHandlerAttributeType, eventHandlerSymbol );
		}

		private static void InspectEventType(
				SyntaxNodeAnalysisContext context,
				InvocationExpressionSyntax invocation,
				INamedTypeSymbol eventAttributeType,
				ITypeSymbol eventTypeSymbol
			) {

			bool hasAttr = eventTypeSymbol
				.GetAttributes()
				.Any( attr => attr.AttributeClass.Equals( eventAttributeType, SymbolEqualityComparer.Default ) );

			if( hasAttr ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.EventTypeMissingEventAttribute,
					invocation.GetLocation(),
					eventTypeSymbol.ToDisplayString()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static void InspectEventHandlerType(
				SyntaxNodeAnalysisContext context,
				InvocationExpressionSyntax invocation,
				INamedTypeSymbol eventHandlerAttributeType,
				ITypeSymbol eventHandlerSymbol
			) {

			bool hasAttr = eventHandlerSymbol
				.GetAttributes()
				.Any( attr => attr.AttributeClass.Equals( eventHandlerAttributeType, SymbolEqualityComparer.Default ) );

			if( hasAttr ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.EventHandlerTypeMissingEventAttribute,
					invocation.GetLocation(),
					eventHandlerSymbol.ToDisplayString()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static IEnumerable<ISymbol> GetGenericRegisterMethods( Compilation compilation ) {

			INamedTypeSymbol registryType = compilation.GetTypeByMetadataName( IEventHandlerRegistryFullName );
			if( registryType.IsNullOrErrorType() ) {
				return Enumerable.Empty<ISymbol>();
			}

			IEnumerable<IMethodSymbol> methods = RegisterMethodNames
				.SelectMany( methodName => registryType.GetMembers( methodName ) )
				.OfType<IMethodSymbol>()
				.Where( m => (
					m.IsGenericMethod
					&& m.TypeArguments.Length == 2
				) );

			return methods;
		}
	}
}
