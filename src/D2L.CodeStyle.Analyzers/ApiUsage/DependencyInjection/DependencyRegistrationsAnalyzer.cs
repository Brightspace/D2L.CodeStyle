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
			"D2L.LP.Extensibility.Activation.Domain.DynamicObjectFactoryRegistryExtensions",
			"D2L.LP.Extensibility.Activation.Domain.IDependencyRegistryExtensions",
			"D2L.LP.Extensibility.Activation.Domain.LegacyPluginsDependencyLoaderExtensions",

			"SpecTests.SomeTestCases.RegistrationCallsInThisClassAreIgnored", // this comes from a test
			"SpecTests.SomeTestCases.RegistrationCallsInThisStructAreIgnored" // this comes from a test
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( 
			Diagnostics.UnsafeSingletonRegistration,
			Diagnostics.RegistrationKindUnknown,
			Diagnostics.AttributeRegistrationMismatch,
			Diagnostics.DependencyRegistraionMissingPublicConstructor
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			DependencyRegistry dependencyRegistry;
			if( !DependencyRegistry.TryCreateRegistry( context.Compilation, out dependencyRegistry ) ) {
				return;
			}

			if( !AnnotationsContext.TryCreate( context.Compilation, out AnnotationsContext annotationsContext ) ) {
				return;
			}
			var immutabilityCtx = ImmutabilityContext.Create( context.Compilation, annotationsContext );

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation( ctx, immutabilityCtx, dependencyRegistry ),
				SyntaxKind.InvocationExpression
			);
		}

		private void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			ImmutabilityContext immutabilityCtx,
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

			InspectRegistration( dependencyRegistration, immutabilityCtx, context );
		}

		private void InspectRegistration(
			DependencyRegistration registration,
			ImmutabilityContext immutabilityCtx,
			SyntaxNodeAnalysisContext ctx
		) {
			if( registration.ObjectScope == ObjectScope.Singleton ) {
				var typesToInspect = GetTypesRequiredToBeImmutableForSingletonRegistration( registration );
				foreach( var type in typesToInspect ) {
					if( type.IsNullOrErrorType() ) {
						continue;
					}

					// We require full immutability here,
					// because we don't know if it's a concrete type
					//
					// TODO: could make this better by exposing the minimum
					// scope required for each type

					var query = new ImmutabilityQuery(
						ImmutableTypeKind.Total,
						type
					);

					if( !immutabilityCtx.IsImmutable( query, () => null, out var _ ) ) {
						var diagnostic = GetUnsafeSingletonDiagnostic(
							ctx.Compilation.Assembly,
							ctx.Node,
							type
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

			if( TryGetInstantiatedTypeForRegistration( registration, out ITypeSymbol instantiatedType ) ) {
				if( !TryGetInjectableConstructor( instantiatedType, out IMethodSymbol injectableConstructor ) ) {
					var diagnostic = Diagnostic.Create(
						Diagnostics.DependencyRegistraionMissingPublicConstructor,
						ctx.Node.GetLocation(),
						instantiatedType.GetFullTypeNameWithGenericArguments()
					);
					ctx.ReportDiagnostic( diagnostic );
				}
			}
		}

		private static Diagnostic GetUnsafeSingletonDiagnostic(
			IAssemblySymbol assembly,
			SyntaxNode syntax,
			ITypeSymbol type
		) {
			DiagnosticSeverity sev;

			switch( assembly.Name ) {
				// To make the cleanup not Sisyphean, the diagnostic is
				// currently ignored in these assemblies:
				case "D2L":
				case "D2L.AW.UserInteractionConsumer":
				case "D2L.Custom.SessionCourseCopy":
				case "D2L.Custom.SpecialAccessAPIs":
				case "D2L.Custom.UserSyncTool":
				case "D2L.eP.Domain":
				case "D2L.LE.IntelligentAgents":
				case "D2L.LE.LO":
				case "D2L.LP":
				case "D2L.LP.AppLoader":
				case "D2L.LP.Diagnostics.Web":
				case "D2L.LP.OrgUnits.WorkQueue":
				case "D2L.LP.Services.Framework":
				case "D2L.LP.Tools.Extensibility":
				case "D2L.LP.Web":
					sev = DiagnosticSeverity.Info;
					break;
				default:
					sev = DiagnosticSeverity.Error;
					break;
			}

			return Diagnostic.Create(
				Diagnostics.UnsafeSingletonRegistration,
				syntax.GetLocation(),
				effectiveSeverity: sev,
				additionalLocations: ImmutableArray<Location>.Empty,
				properties: ImmutableDictionary<string, string>.Empty,
				type.GetFullTypeNameWithGenericArguments()
			);
		}

		private ImmutableArray<ITypeSymbol> GetTypesRequiredToBeImmutableForSingletonRegistration( DependencyRegistration registration ) {
			// if we have a concrete type, use it
			if( !registration.ConcreteType.IsNullOrErrorType() ) {
				return ImmutableArray.Create( registration.ConcreteType );
			}

			// otherwise use the dependency type
			return ImmutableArray.Create( registration.DependencyType );
		}

		/// <summary>
		/// Gets the type that will be actually instantiated by DI for the registration.
		/// This is the factory type for registrations like RegisterFactory, or the concrete type otherwise
		/// </summary>
		/// <param name="registration"></param>
		/// <param name="instantiatedType"></param>
		/// <returns></returns>
		private bool TryGetInstantiatedTypeForRegistration( DependencyRegistration registration, out ITypeSymbol instantiatedType ) {
			ITypeSymbol type = registration.FactoryType ?? registration.ConcreteType;

			if( type.IsNullOrErrorType() ) {
				instantiatedType = null;
				return false;
			}

			// hacks
			// handle RegisterSubInterface<IFoo, IFooBar>():
			// IFooBar is in the registration as "concrete type" and don't want to deal with that refactoring
			if( type.TypeKind == TypeKind.Interface ) {
				instantiatedType = null;
				return false;
			}

			// ignore registry usage in registry extensions we don't know about
			// public static void Foo<T, U>( this IDependencyRegistry @this, ObjectScope scope ) where U : T {
			//   // Can't find constructor for U!
			//   @this.Register<T, U>( scope );
			// }
			if( type.TypeKind == TypeKind.TypeParameter ) {
				instantiatedType = null;
				return false;
			}

			// Register( typof( IFoo<> ), typeof( Foo<> ), ObjectScope.Singleton )
			// Turn Foo<> into Foo<T>
			if( type is INamedTypeSymbol namedType ) {
				type = namedType.ConstructedFrom;
			}

			instantiatedType = type;
			return true;
		}

		private bool TryGetInjectableConstructor( ITypeSymbol type, out IMethodSymbol injectableConstructor ) {
			injectableConstructor = type.GetMembers()
				.Where( m => m.Kind == SymbolKind.Method )
				.Cast<IMethodSymbol>()
				.Where( m => m.MethodKind == MethodKind.Constructor && m.DeclaredAccessibility == Accessibility.Public )
				.FirstOrDefault();

			if( injectableConstructor == null ) {
				return false;
			}

			return true;
		}

		private bool TryGetDependenciesFromConstructor( ITypeSymbol type, out ImmutableArray<ITypeSymbol> dependencies  ) {
			if( !TryGetInjectableConstructor( type, out IMethodSymbol ctor ) ) {
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
