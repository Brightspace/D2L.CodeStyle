using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Analyzers.Immutability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ImmutableGenericDeclarationAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.GenericArgumentTypeMustBeImmutable );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalyzer );
		}

		private static void RegisterAnalyzer(
			CompilationStartAnalysisContext context
		) {
			context.RegisterSymbolAction(
				ctx => AnalyzeTypeDeclaration(
					ctx
				),
				SymbolKind.NamedType );

			context.RegisterSymbolAction(
				ctx => AnalyzeFieldOrProperty(
					ctx
				),
				new SymbolKind[] { SymbolKind.Field, SymbolKind.Property } );
		}

		private static void AnalyzeFieldOrProperty(
			SymbolAnalysisContext context
		) {
			ITypeSymbol symbol;
			SymbolKind symbolKind = context.Symbol.Kind;
			switch( symbolKind ) {
				case SymbolKind.Field:
					symbol = ( context.Symbol as IFieldSymbol ).Type;
					break;
				case SymbolKind.Property:
					symbol = ( context.Symbol as IPropertySymbol ).Type;
					break;
				default:
					throw new InvalidOperationException();
			}

			if( symbol is IErrorTypeSymbol ) {
				return;
			}

			PerformGenericArgumentInspection( context, symbol );
		}

		private static void AnalyzeTypeDeclaration(
			SymbolAnalysisContext context
		) {
			var symbol = (INamedTypeSymbol)context.Symbol;

			if( !symbol.IsDefinition ) {
				return;
			}

			PerformGenericArgumentInspection( context, symbol );
		}

		private static void PerformGenericArgumentInspection(
			SymbolAnalysisContext context,
			ITypeSymbol type
		) {
			if( type.IsGenericType() ) {
				// when the generic type is inline with the declaration 'Foo<Mutable>'
				InspectGenericArguments( context, type, type );
			} else if( type.BaseType.IsGenericType() ) {
				// when the generic type is part of the base declaration 'Foo: Bar<Mutable>'
				InspectGenericArguments( context, type.BaseType, type );

			}

			InspectInterfaceArguments( context, type );
		}

		private static void InspectInterfaceArguments(
			SymbolAnalysisContext context,
			ITypeSymbol type
		) {

			foreach( INamedTypeSymbol intf in type.Interfaces ) {
				if( intf.IsGenericType() ) {
					InspectGenericArguments( context, intf, type );
				}
			}
		}

		private static void InspectGenericArguments(
			SymbolAnalysisContext context,
			ITypeSymbol type,
			ITypeSymbol hostType
		) {
			if( type.IsGenericType() ) {
				var symbolType = type as INamedTypeSymbol;
				if( symbolType == default ) {
					return;
				}

				int index = 0;
				foreach( ITypeSymbol argument in symbolType.TypeArguments ) {
					ImmutabilityScope argumentScope =
						symbolType.TypeParameters[index].GetImmutabilityScope();

					if( argumentScope == ImmutabilityScope.SelfAndChildren ) {
						ImmutabilityScope targetScope = argument.GetImmutabilityScope();

						if( targetScope != ImmutabilityScope.SelfAndChildren ) {

							Location location = GetLocation( context, type, hostType );

							context.ReportDiagnostic( Diagnostic.Create(
								Diagnostics.GenericArgumentTypeMustBeImmutable,
								location,
								messageArgs: new object[] { argument.Name } ) );
						}
					}

					index++;  // Advances the arguments with the parameters in lock-step
				}
			}
		}

		private static Location GetLocation(
			SymbolAnalysisContext context,
			ITypeSymbol type,
			ITypeSymbol hostType
		) {
			Location location;
			if( hostType.IsValueType ) {
				// Declarations like 'struct Foo: IFoo<Bar>'
				location = hostType.GetDeclarationSyntax<StructDeclarationSyntax>().BaseList.GetLocation();
			} else {
				if( hostType == type ) {
					// Fields and properties
					location = context.Symbol.DeclaringSyntaxReferences.First().GetSyntax().GetLocation();
				} else {
					// Declarations like 'class Foo: IFoo<Bar>'
					location = hostType.GetDeclarationSyntax<ClassDeclarationSyntax>().BaseList.GetLocation();
				}
			}

			return location;
		}

	}
}