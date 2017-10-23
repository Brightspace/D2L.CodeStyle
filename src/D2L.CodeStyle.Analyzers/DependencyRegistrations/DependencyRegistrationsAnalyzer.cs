using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using D2L.CodeStyle.Analyzers.Common.DependencyInjection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.DependencyRegistrations {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class DependencyRegistrationsAnalyzer : DiagnosticAnalyzer {

		// It might be worthwhile to refactor this to an attribute instead later.
		private static readonly IImmutableSet<string> s_blessedClasses = ImmutableHashSet.Create(
			"D2L.LP.Extensibility.Activation.Domain.DependencyRegistryExtensionPointExtensions",
			"D2L.LP.Extensibility.Activation.Domain.DynamicObjectFactoryRegistryExtensions",
			"D2L.LP.Extensibility.Activation.Domain.IDependencyRegistryConfigurePluginsExtensions",
			"D2L.LP.Extensibility.Activation.Domain.IDependencyRegistryExtensions",
			"D2L.LP.Extensibility.Activation.Domain.LegacyPluginsDependencyLoaderExtensions",

			"SpecTests.SomeTestCases.RegistrationCallsInThisClassAreIgnored", // this comes from a test
			"SpecTests.SomeTestCases.RegistrationCallsInThisStructAreIgnored" // this comes from a test
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( 
			Diagnostics.UnsafeSingletonRegistration,
			Diagnostics.SingletonRegistrationTypeUnknown,
			Diagnostics.RegistrationKindUnknown
		);

		private readonly MutabilityInspectionResultFormatter m_resultFormatter = new MutabilityInspectionResultFormatter();

		public override void Initialize( AnalysisContext context ) {
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			var inspector = new MutabilityInspector( new KnownImmutableTypes( context.Compilation.Assembly ) );

			DependencyRegistry dependencyRegistry;
			if( !DependencyRegistry.TryCreateRegistry( context.Compilation, out dependencyRegistry ) ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation( ctx, inspector, dependencyRegistry ),
				SyntaxKind.InvocationExpression
			);
		}

		private void AnalyzeInvocation( SyntaxNodeAnalysisContext context, MutabilityInspector inspector, DependencyRegistry registry ) {
			var root = context.Node as InvocationExpressionSyntax;
			if( root == null ) {
				return;
			}
			var method = context.SemanticModel.GetSymbolInfo( root ).Symbol as IMethodSymbol;
			if( method == null ) {
				return;
			}

			if( !registry.IsRegistationMethod( method ) ) {
				return;
			}

			if( IsExpressionInClassInIgnoreList( root, context.SemanticModel ) ) {
				return;
			}

			DependencyRegistrationExpression dependencyRegistrationExpresion;
			if( !registry.TryMapRegistrationMethod( 
				method, 
				root.ArgumentList.Arguments, 
				context.SemanticModel, 
				out dependencyRegistrationExpresion 
			) ) {
				// this can happen where there's a new registration method
				// that we can't map to
				// so we fail
				var diagnostic = Diagnostic.Create(
					Diagnostics.RegistrationKindUnknown,
					root.GetLocation()
				);
				context.ReportDiagnostic( diagnostic );
				return;
			}

			var dependencyRegistration = dependencyRegistrationExpresion.GetRegistration( 
				method,
				root.ArgumentList.Arguments,
				context.SemanticModel
			);
			if( dependencyRegistration == null ) {
				/* This can happen in the following scenarios:
				 * 1) ObjectScope is passed in as a variable and we can't extract it.
				 * 2) Some require argument is missing (compile error).
				 * We fail because we can't analyze it.
				 */
				var diagnostic = Diagnostic.Create(
					Diagnostics.RegistrationKindUnknown,
					root.GetLocation()
				);
				context.ReportDiagnostic( diagnostic );
				return;
			}

			if( dependencyRegistration.ObjectScope != ObjectScope.Singleton ) {
				// we only care about singletons
				return;
			}

			var typeToInspect = GetTypeToInspect( dependencyRegistration );
			if( typeToInspect.IsNullOrErrorType() ) {
				// we expected a type, but didn't get one, so fail
				var diagnostic = Diagnostic.Create(
					Diagnostics.SingletonRegistrationTypeUnknown,
					root.GetLocation()
				);
				context.ReportDiagnostic( diagnostic );
				return;
			}

			var isMarkedSingleton = inspector.IsTypeMarkedSingleton( typeToInspect );
			if( !isMarkedSingleton ) {
				var diagnostic = Diagnostic.Create(
					Diagnostics.UnsafeSingletonRegistration,
					root.GetLocation(),
					typeToInspect.GetFullTypeNameWithGenericArguments()
				);
				context.ReportDiagnostic( diagnostic );
			}
		}

		private ITypeSymbol GetTypeToInspect( DependencyRegistration registration ) {
			// if we have a concrete type, use it; otherwise, use the dependency type
			if( !registration.IsFactoryRegistration && !registration.ConcreteType.IsNullOrErrorType() ) {
				return registration.ConcreteType;
			}
			return registration.DependencyType;
		}

		private bool IsExpressionInClassInIgnoreList( InvocationExpressionSyntax expr, SemanticModel semanticModel ) {
			var structOrClass = GetClassOrStructContainingExpression( expr, semanticModel );
			if( structOrClass.IsNullOrErrorType() ) {
				// we failed to pull out the class/struct this invocation is being called from
				// so don't ignore it
				return false;
			}

			var className = structOrClass.GetFullTypeNameWithGenericArguments();
			if( s_blessedClasses.Contains( className ) ) {
				return true;
			}

			return false;
		}

		private ITypeSymbol GetClassOrStructContainingExpression(
			InvocationExpressionSyntax expr,
			SemanticModel semanticModel
		) {
			foreach( var ancestor in expr.Ancestors() ) {
				if( ancestor is StructDeclarationSyntax ) {
					return semanticModel.GetDeclaredSymbol( ancestor as StructDeclarationSyntax );
				} else if( ancestor is ClassDeclarationSyntax ) {
					return semanticModel.GetDeclaredSymbol( ancestor as ClassDeclarationSyntax );
				}
			}
			return null;
		}
	}
}
