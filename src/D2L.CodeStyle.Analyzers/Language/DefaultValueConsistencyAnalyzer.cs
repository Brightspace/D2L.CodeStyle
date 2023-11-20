#nullable enable

using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class DefaultValueConsistencyAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.IncludeDefaultValueInOverrideForReadability,
				Diagnostics.DontIntroduceNewDefaultValuesInOverrides,
				Diagnostics.DefaultValuesInOverridesShouldBeConsistent
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );

			context.RegisterSymbolAction(
				static ctx => AnalyzeNamedType( ctx, (INamedTypeSymbol)ctx.Symbol ),
				SymbolKind.NamedType
			);
		}

		private static void AnalyzeNamedType(
			SymbolAnalysisContext ctx,
			INamedTypeSymbol type
		) {
			if( type.TypeKind == TypeKind.Interface ) {
				return;
			}

			// Check for base class overrides
			foreach( ISymbol member in type.GetMembers() ) {
				if( member is not IMethodSymbol method ) {
					continue;
				}

				// Explicit implementations aren't overriding anything on the base type, so ignore them
				if( !method.ExplicitInterfaceImplementations.IsEmpty ) {
					continue;
				}

				if( method.OverriddenMethod is null ) {
					continue;
				}

				// Ignore overloads of System.Object methods (such as Equals) that we don't care about
				if( SymbolEqualityComparer.Default.Equals( method.OverriddenMethod.ContainingType, ctx.Compilation.ObjectType ) ) {
					continue;
				}

				// Ignore overloads of System.ValueType methods (such as Equals) that we don't care about
				if( SymbolEqualityComparer.Default.Equals( method.OverriddenMethod.ContainingType, ctx.Compilation.GetSpecialType( SpecialType.System_ValueType ) ) ) {
					continue;
				}

				AnalyzeMethod(
					ctx.ReportDiagnostic,
					baseMethod: method.OverriddenMethod,
					implMethod: method,
					locationOverride: null,
					ctx.CancellationToken
				);
			}

			// Check for interface implementations, which may come from base types
			foreach( INamedTypeSymbol @interface in type.AllInterfaces ) {
				bool? interfaceIsFromBaseType = null;
				foreach( ISymbol member in @interface.GetMembers() ) {
					if( member is not IMethodSymbol method ) {
						continue;
					}

					ISymbol? implementingMember = type.FindImplementationForInterfaceMember( method );

					// Should always be a method symbol
					if( implementingMember is not IMethodSymbol implementingMethod ) {
						continue;
					}

					// Explicit implementations can't change default value
					// behaviour, so we don't need to worry about them.
					if( !implementingMethod.ExplicitInterfaceImplementations.IsEmpty ) {
						continue;
					}

					bool methodIsImplementedLocally = SymbolEqualityComparer.Default.Equals( type, implementingMethod.ContainingType );
					if( !methodIsImplementedLocally ) {

						// If the method and interface come from the base type then any diagnostics
						// will be raised when analyzing the base type, skip analyzing the method
						interfaceIsFromBaseType ??= type.BaseType!.AllInterfaces.Contains( @interface, SymbolEqualityComparer.Default );
						if( interfaceIsFromBaseType.Value == true ) {
							continue;
						}
					}

					AnalyzeMethod(
						ctx.ReportDiagnostic,
						baseMethod: method,
						implMethod: implementingMethod,
						locationOverride: methodIsImplementedLocally ? null : GetTypeNameDiagnosticLocation( ctx, type ),
						ctx.CancellationToken
					);
				}
			}
		}

		private static void AnalyzeMethod(
			Action<Diagnostic> reportDiagnostic,
			IMethodSymbol baseMethod,
			IMethodSymbol implMethod,
			Location? locationOverride,
			CancellationToken cancellationToken
		) {
			foreach( var implParameter in implMethod.Parameters ) {
				// The order of parameters in both methods will line up
				var baseParameter = baseMethod
					.Parameters.First(
						p => p.Ordinal == implParameter.Ordinal
					);

				var hasDefault = implParameter.HasExplicitDefaultValue;
				var shouldHaveDefault = baseParameter.HasExplicitDefaultValue;

				// the best case: no default values
				if( !hasDefault && !shouldHaveDefault ) {
					continue;
				}

				if( !hasDefault && shouldHaveDefault ) {
					// It makes the implementation more readable if it
					// duplicates the default value from the original
					// definition. Additionally it removes inconsistency
					// in using a reference to the impl vs. base.
					reportDiagnostic(
						Diagnostic.Create(
							Diagnostics.IncludeDefaultValueInOverrideForReadability,
							locationOverride ?? GetLocation( implParameter, cancellationToken ),
							implParameter.Name,
							baseMethod.ContainingType.Name
						)
					);

					continue;
				}

				if( hasDefault && !shouldHaveDefault ) {
					// Giving default values only for the impl leads to
					// inconsistent behaviour that we don't like.
					reportDiagnostic(
						Diagnostic.Create(
							Diagnostics.DontIntroduceNewDefaultValuesInOverrides,
							locationOverride ?? GetLocation( implParameter, cancellationToken ),
							implParameter.Name,
							baseMethod.ContainingType.Name
						)
					);

					continue;
				}

				// therefore, hasDefault && shouldHaveDefault. Make sure that
				// they used the same default value.

				var implDefault = implParameter.ExplicitDefaultValue;
				var baseDefault = baseParameter.ExplicitDefaultValue;

				// Use the static object.Equals because implDefault could
				// legtimately be null and implDefault.Equals( baseDefault )
				// would throw a NRE.
				if( !Equals( implDefault, baseDefault ) ) {
					// Inconsistent default values are VERY confusing. It
					// almost surely isn't intentional; it usually happens
					// because someone wants to change the default but doesn't
					// update everything.

					reportDiagnostic(
						Diagnostic.Create(
							Diagnostics.DefaultValuesInOverridesShouldBeConsistent,
							locationOverride ?? GetLocation( implParameter, cancellationToken ),
							implParameter.Name,
							FormatDefaultValue( implDefault ),
							FormatDefaultValue( baseDefault ),
							baseMethod.ContainingType.Name
						)
					);
				}
			}
		}

		private static string FormatDefaultValue( object? val ) {
			if ( val == null ) {
				return "null";
			}

			if ( val is string s ) {
				return $@"""{s}""";
			}

			return val.ToString();
		}

		private static Location GetLocation( IParameterSymbol param, CancellationToken cancellationToken ) {
			return param
				.DeclaringSyntaxReferences[0]
				.GetSyntax( cancellationToken )
				.GetLocation();
		}

		private static Location GetTypeNameDiagnosticLocation( SymbolAnalysisContext ctx, INamedTypeSymbol namedType ) {
			var (type, baseType) = namedType.ExpensiveGetSyntaxImplementingType( namedType.BaseType!, ctx.Compilation, ctx.CancellationToken );

			if( baseType is not null ) {
				return baseType.GetLocation();
			}

			if( type is not null ) {
				return type.Identifier.GetLocation();
			}

			return Location.None;
		}
	}
}
