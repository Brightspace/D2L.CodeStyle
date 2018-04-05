using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.CustomerState {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class CustomerStateAnalyzer : DiagnosticAnalyzer {

		public enum CustomerStateResult {
			Unknown,

			NoCustomerState,

			CustomerState
		}

		public class CustomerStateAnalysis {
			public CustomerStateResult State { get; set; }

			public bool IsHidden { get; set; }
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.SingletonDependencyHasCustomerState,
			Diagnostics.PublicClassHasHiddenCustomerState
		);

		public override void Initialize(
			AnalysisContext context
		) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis(
			CompilationStartAnalysisContext context
		) {
			var singletonAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.LP.Extensibility.Activation.Domain.SingletonAttribute" );

			var customerStateAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.CodeStyle.Annotations.Attributes.CustomerStateAttribute" );

			// Don't proceed if we couldn't resolve either of the two
			// attributes we're going to use otherwise we'll waste time
			// performing an analysis that won't do anything.
			if (singletonAttribute.IsNullOrErrorType()
				|| customerStateAttribute.IsNullOrErrorType()) {
				return;
			}

			var previousAnalysis = new ConcurrentDictionary<ITypeSymbol, CustomerStateAnalysis>();
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeClass(
					ctx,
					singletonAttribute,
					customerStateAttribute,
					previousAnalysis
				),
				SyntaxKind.ClassDeclaration
			);
		}

		private void AnalyzeClass(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol singletonAttribute,
			INamedTypeSymbol customerStateAttribute,
			ConcurrentDictionary<ITypeSymbol, CustomerStateAnalysis> previousAnalysis
		) {
			var root = (ClassDeclarationSyntax)context.Node;

			INamedTypeSymbol symbol = context.SemanticModel.GetDeclaredSymbol( root );
			if( symbol == default( INamedTypeSymbol ) ) {
				return;
			}

			// We only emit the singleton customer state diagnostics for 
			// type chains that are part of singleton analysis, otherwise
			// we can emit the hidden state diagnostic
			var inSingletonChain = symbol.IsTypeMarkedWith( singletonAttribute );

			if( previousAnalysis.TryGetValue( symbol, out CustomerStateAnalysis analysis ) ) {
				if( ( analysis.State == CustomerStateResult.CustomerState )
					&& inSingletonChain
				) {
					Location location = root.Identifier.GetLocation();

					Diagnostic diagnostic = Diagnostic.Create(
						Diagnostics.SingletonDependencyHasCustomerState,
						location );

					context.ReportDiagnostic( diagnostic );
				}

				if( analysis.IsHidden ) {
					Location location = root.Identifier.GetLocation();

					Diagnostic diagnostic = Diagnostic.Create(
						Diagnostics.PublicClassHasHiddenCustomerState,
						location );

					context.ReportDiagnostic( diagnostic );
				}

				return;
			}

			var inspectedTypes = new HashSet<ITypeSymbol>();
			AnalyzeType(
				context,
				root,
				symbol,
				inSingletonChain,
				customerStateAttribute,
				inspectedTypes,
				previousAnalysis );
		}

		private CustomerStateResult AnalyzeType(
			SyntaxNodeAnalysisContext context,
			ClassDeclarationSyntax root,
			ITypeSymbol type,
			bool inSingletonChain,
			INamedTypeSymbol customerStateAttribute,
			HashSet<ITypeSymbol> inspectedTypes,
			ConcurrentDictionary<ITypeSymbol, CustomerStateAnalysis> previousAnalysis
		) {

			// Note the current inspection, otherwise we could end up
			// in a type-loop and a resulting StackOverflowException
			if( inspectedTypes.Contains( type ) ) {
				// We can return Unknown here because we only get in here in
				// a loop and therefore a previous analysis of this class is
				// pending and will be unwound when we return and the
				// "corect" value will be stored.
				return CustomerStateResult.Unknown;
			}

			inspectedTypes.Add( type );
			var result = CustomerStateResult.NoCustomerState;
			var hasHiddenState = false;

			if( type.IsTypeMarkedWith( customerStateAttribute ) ) {
				if( inSingletonChain ) {
					Location location = root.Identifier.GetLocation();

					Diagnostic diagnostic = Diagnostic.Create(
						Diagnostics.SingletonDependencyHasCustomerState,
						location );

					context.ReportDiagnostic( diagnostic );
				}

				result = CustomerStateResult.CustomerState;
			}

			foreach( ISymbol member in type.GetMembers() ) {
				if( member is IErrorTypeSymbol ) {
					continue;
				}

				foreach( var memberSymbol in GetTypeSymbols( member ) ) {
					switch( memberSymbol.TypeKind ) {
						case TypeKind.Class:
						case TypeKind.Struct:
						case TypeKind.Interface:
							var subResult = AnalyzeType(
								context,
								root,
								memberSymbol,
								inSingletonChain,
								customerStateAttribute,
								inspectedTypes,
								previousAnalysis );

							// If any sub-type contains customer state, then
							// we contain customer state.
							if( subResult == CustomerStateResult.CustomerState ) {
								result = CustomerStateResult.CustomerState;

								if( ( memberSymbol.DeclaredAccessibility != Accessibility.Public )
									&& ( type.DeclaredAccessibility == Accessibility.Public )
									&& ( !type.IsTypeMarkedWith( customerStateAttribute ) ) ) {

									hasHiddenState = true;
								}
							}
							break;
					}
				}
			}

			if( hasHiddenState ) {
				Location location = root.Identifier.GetLocation();

				Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.PublicClassHasHiddenCustomerState,
					location );

				context.ReportDiagnostic( diagnostic );
			}

			previousAnalysis.TryAdd( type, new CustomerStateAnalysis {
				State = result,
				IsHidden = hasHiddenState
			} );

			return result;
		}

		private IEnumerable<ITypeSymbol> GetTypeSymbols(
			ISymbol symbol
		) {
			var result = new List<ITypeSymbol>();

			// Get the type symbol for the current symbol if it is of
			// interest.
			ITypeSymbol typeSymbol = default( ITypeSymbol );
			switch( symbol.Kind ) {
				case SymbolKind.Field:
					typeSymbol = ( (IFieldSymbol)symbol ).Type;
					break;
				case SymbolKind.Property:
					typeSymbol = ( (IPropertySymbol)symbol ).Type;
					break;
				default:
					// We're not interested...bail out now.
					return result;
			}

			if( ( typeSymbol is INamedTypeSymbol nts ) && nts.IsGenericType ) {
				// Basically if we can decompose type aggregated generic type
				// then we'll return the base generic type and all of its
				// type arguments
				result.Add( nts.OriginalDefinition as ITypeSymbol );
				result.AddRange( nts.TypeArguments );

			} else {
				// Otherwise we just return symbol for the type we're scanning
				result.Add( typeSymbol );
			}

			// So, if it's not a field, not a property and not a generic
			// type this will be an empty list, otherwise it contains
			// all the type of interest that need to be recursively scanned.
			return result;
		}
	}
}