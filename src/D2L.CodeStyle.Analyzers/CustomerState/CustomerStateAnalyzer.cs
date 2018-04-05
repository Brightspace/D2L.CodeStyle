using System;
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
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.SingletonDependencyHasCustomerState
		);

		public override void Initialize(
			AnalysisContext context
		) {
			//context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis(
			CompilationStartAnalysisContext context
		) {
			var singletonAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.LP.Extensibility.Activation.Domain.SingletonAttribute" );

			var customerStateAttribute = context.Compilation.GetTypeByMetadataName(
				"D2L.LP.Extensibility.Activation.Domain.CustomerStateAttribute" );

			if( singletonAttribute == null
				|| singletonAttribute.Kind == SymbolKind.ErrorType
			) {
				return;
			}

			if( customerStateAttribute == null
				|| customerStateAttribute.Kind == SymbolKind.ErrorType
			) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeClass(
					ctx,
					singletonAttribute,
					customerStateAttribute
				),
				SyntaxKind.ClassDeclaration
			);
		}

		private void AnalyzeClass(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol singletonAttribute,
			INamedTypeSymbol customerStateAttribute
		) {
			var root = context.Node as ClassDeclarationSyntax;
			if( root == null ) {
				return;
			}

			var symbol = context.SemanticModel.GetDeclaredSymbol( root );
			if( symbol == null ) {
				return;
			}

			// skip classes not marked singleton
			if( !symbol.IsTypeMarkedSingleton() ) {
				return;
			}

			var inspectedTypes = new HashSet<ITypeSymbol>();
			AnalyzeType( context, root, symbol, customerStateAttribute, inspectedTypes );
		}

		private void AnalyzeType(
			SyntaxNodeAnalysisContext context,
			ClassDeclarationSyntax root,
			ITypeSymbol type,
			INamedTypeSymbol customerStateAttribute,
			HashSet<ITypeSymbol> inspectedTypes
		) {

			// Note the current inspection, otherwise we could end up
			// in a type-loop and a resulting StackOverflowException
			if( inspectedTypes.Contains(type)) {
				return;
			}
			inspectedTypes.Add( type );

			if( type.IsTypeMarkedWith( customerStateAttribute ) ) {
				var location = root.Identifier.GetLocation();

				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.SingletonDependencyHasCustomerState,
					location
				) );
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
							AnalyzeType(
								context,
								root,
								memberSymbol,
								customerStateAttribute,
								inspectedTypes );
							break;
					}
				}
			}
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

			// Basically if we can decompose type aggregated generic type
			// then we'll return the base generic type and all of its
			// type arguments
			if( ( typeSymbol is INamedTypeSymbol nts ) && nts.IsGenericType ) {
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