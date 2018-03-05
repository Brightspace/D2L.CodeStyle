using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Analyzers.Immutability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection {
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
			Diagnostics.RegistrationKindUnknown,
			Diagnostics.AttributeRegistrationMismatch
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			DependencyRegistry dependencyRegistry;
			if( !DependencyRegistry.TryCreateRegistry( context.Compilation, out dependencyRegistry ) ) {
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation( ctx, dependencyRegistry ),
				SyntaxKind.InvocationExpression
			);
		}

		private void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			DependencyRegistry registry
		) {
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

			InspectRegistration( dependencyRegistration, context );
		}

		private void InspectRegistration( DependencyRegistration registration, SyntaxNodeAnalysisContext ctx ) {
			if( registration.ObjectScope == ObjectScope.Singleton ) {
				var typesToInspect = GetTypesRequiredToBeImmutableForSingletonRegistration( registration );
				foreach( var type in typesToInspect ) {

					// We require full immutability here,
					// because we don't know if it's a concrete type
					//
					// TODO: could make this better by exposing the minimum
					// scope required for each type
					var immutabilityScope = type.GetImmutabilityScope();
					if( !type.IsNullOrErrorType() && immutabilityScope != ImmutabilityScope.SelfAndChildren ) {
						var diagnostic = Diagnostic.Create(
							Diagnostics.UnsafeSingletonRegistration,
							ctx.Node.GetLocation(),
							type.GetFullTypeNameWithGenericArguments()
						);
						ctx.ReportDiagnostic( diagnostic );
					}
				}
			}

			// ensure webrequest isn't marked Singleton
			var isMarkedSingleton = registration.DependencyType.IsTypeMarkedSingleton();
			if( isMarkedSingleton && registration.ObjectScope != ObjectScope.Singleton ) {
				var diagnostic = Diagnostic.Create(
					Diagnostics.AttributeRegistrationMismatch,
					ctx.Node.GetLocation(),
					registration.DependencyType.GetFullTypeNameWithGenericArguments()
				);
				ctx.ReportDiagnostic( diagnostic );
			}

		}

		private ImmutableArray<ITypeSymbol> GetTypesRequiredToBeImmutableForSingletonRegistration( DependencyRegistration registration ) {
			// if we have a concrete type, use it
			if( !registration.ConcreteType.IsNullOrErrorType() ) {
				return ImmutableArray.Create( registration.ConcreteType );
			}

			// if we have a dynamically generated objectfactory, use its constructor arguments
			if( !registration.DynamicObjectFactoryType.IsNullOrErrorType() ) {
				ImmutableArray<ITypeSymbol> dependencies;
				if( TryGetDependenciesFromConstructor( registration.DynamicObjectFactoryType, out dependencies ) ) {
					return dependencies;
				}
				// this is a dynamic object factory, but either 
				// (1) there is no public constructor, or 
				// (2) one of the parameter's types doesn't exist
				// in all cases, fall back to dependency type
			}

			// otherwise use the dependency type
			return ImmutableArray.Create( registration.DependencyType );
		}

		private bool TryGetDependenciesFromConstructor( ITypeSymbol type, out ImmutableArray<ITypeSymbol> dependencies  ) {
			var ctor = type.GetMembers()
				.Where( m => m.Kind == SymbolKind.Method )
				.Cast<IMethodSymbol>()
				.Where( m => m.MethodKind == MethodKind.Constructor && m.DeclaredAccessibility == Accessibility.Public )
				.FirstOrDefault();
			if( ctor == null ) {
				dependencies = ImmutableArray<ITypeSymbol>.Empty;
				return false;
			}

			dependencies = ctor.Parameters
				.Where( Attributes.Dependency.IsDefined )
				.Select( p => p.Type )
				.ToImmutableArray();
			return true;
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
