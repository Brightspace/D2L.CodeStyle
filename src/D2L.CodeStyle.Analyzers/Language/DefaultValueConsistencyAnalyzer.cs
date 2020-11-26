using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

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

			// Check for methods with the "override" modifier
			context.RegisterSyntaxNodeAction(
				AnalyzeOverrideMethodDeclaration,
				SyntaxKind.MethodDeclaration
			);

			// We also need to check interface method impls, and unfortunately
			// this is the best way I know how to do it:
			context.RegisterSyntaxNodeAction(
				AnalyzeBaseList,
				SyntaxKind.BaseList
			);
		}

		private static void AnalyzeOverrideMethodDeclaration(
			SyntaxNodeAnalysisContext ctx
		) {
			var methodDecl = (MethodDeclarationSyntax)ctx.Node;

			var hostDecl = methodDecl.Parent as TypeDeclarationSyntax;

			// The parser may create MethodDeclaration nodes in weird places
			// (e.g. as a member of a NamespaceDeclaration) just to continue
			// on parsing and generating diagnostics. The only sensible places
			// are TypeDeclarationSyntaxs (class, struct or interface decls)
			// so just bail out if we're in a weird situation.
			if ( hostDecl == null ) {
				return;
			}

			// As a performance optimization we can bail out early if there
			// are no overrides we care about (e.g. our base class is either
			// System.Object or System.ValueType and we don't implement any
			// interfaces.) This saves querying the semantic model.
			if ( hostDecl.BaseList == null ) {
				return;
			}

			var method = ctx.SemanticModel
				.GetDeclaredSymbol( methodDecl );

			if ( method == null || method.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if( !method.IsOverride ) {
				return;
			}

			// There might be other build errors (e.g. from missing partial
			// classes from code-gen, or incomplete code when we're running
			// in intellisense) preventing the semantic model from locating
			// the original method. Ignore these.
			if( method.OverriddenMethod == null ) {
				return;
			}

			AnalyzeMethod(
				ctx.ReportDiagnostic,
				baseMethod: method.OverriddenMethod,
				implMethod: method
			);
		}

		private static void AnalyzeBaseList( SyntaxNodeAnalysisContext ctx ) {
			var baseList = (BaseListSyntax)ctx.Node;

			// ignore enums, their BaseListSyntax isn't relevant
			if ( baseList.Parent is EnumDeclarationSyntax ) {
				return;
			}

			// Not likely, maybe as the user is typing stuff out.
			if ( baseList.Types.Count == 0 ) {
				return;
			}

			var implType = ctx.SemanticModel
				.GetDeclaredSymbol( (TypeDeclarationSyntax)baseList.Parent );

			// The most expensive thing we do:
			var interfaceMethodsAndImpls = implType
				.AllInterfaces // interfaces can extend interfaces, etc.
				.SelectMany( iface => iface.GetMembers() )
				.OfType<IMethodSymbol>()
				.Select( ifaceMethod =>
					(ifaceMethod, implType.FindImplementationForInterfaceMember( ifaceMethod ) as IMethodSymbol )
				);

			foreach( (var ifaceMethod, var implMethod) in interfaceMethodsAndImpls ) {
				if ( implMethod == null ) {
					// Maybe they implemented a method with a non-method (which
					// would be a different build error) or they haven't
					// implemented it yet.
					continue;
				}

				if ( !implMethod.ContainingType.Equals( implType, SymbolEqualityComparer.Default ) ) {
					// Our base class could implement the method. We don't want
					// to duplicate the work on principle but also when we look
					// at DeclaringSyntaxReferences in GetLocation it could
					// fail because our base could be in a different assembly.
					return;
				}

				// This is O(# of explicit implementations for this method), in
				// a loop which includes at least that many things, so O(n^2)
				// for some n. But n is probably small.
				if ( implMethod.ExplicitInterfaceImplementations.Contains( ifaceMethod ) ) {
					// Explicit implementations can't change default value
					// behaviour, so we don't need to worry about them.
					continue;
				}

				AnalyzeMethod(
					ctx.ReportDiagnostic,
					baseMethod: ifaceMethod,
					implMethod: implMethod
				);
			}
		}

		private static void AnalyzeMethod(
			Action<Diagnostic> reportDiagnostic,
			IMethodSymbol baseMethod,
			IMethodSymbol implMethod
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
							GetLocation( implParameter ),
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
							GetLocation( implParameter ),
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
							GetLocation( implParameter ),
							implParameter.Name,
							FormatDefaultValue( implDefault ),
							FormatDefaultValue( baseDefault ),
							baseMethod.ContainingType.Name
						)
					);
				}
			}
		}

		private static string FormatDefaultValue( object val ) {
			if ( val == null ) {
				return "null";
			}

			if ( val is string s ) {
				return $@"""{s}""";
			}

			return val.ToString();
		}

		private static Location GetLocation( IParameterSymbol param ) {
			return param
				.DeclaringSyntaxReferences[0]
				.GetSyntax()
				.GetLocation();
		}
	}
}
