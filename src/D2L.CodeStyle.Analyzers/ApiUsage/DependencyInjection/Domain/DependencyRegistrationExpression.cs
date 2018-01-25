using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	internal abstract class DependencyRegistrationExpression {

		protected const string IFactoryTypeMetadataName = "D2L.LP.Extensibility.Activation.Domain.IFactory`1";

		internal abstract bool CanHandleMethod(
			IMethodSymbol method
		);

		internal abstract DependencyRegistration GetRegistration(
			IMethodSymbol method, 
			SeparatedSyntaxList<ArgumentSyntax> arguments, 
			SemanticModel semanticModel
		);

		protected bool TryGetObjectScope(
			ArgumentSyntax argument,
			SemanticModel semanticModel,
			out ObjectScope scope
		) {
			scope = ObjectScope.AlwaysCreateNewInstance; // bogus

			var scopeArgumentValue = semanticModel.GetConstantValue( argument.Expression );
			if( !scopeArgumentValue.HasValue ) {
				// this can happen if someone is typing, or in the rare case that someone doesn't pass this value inline (i.e., uses a variable)
				return false;
			}

			// if this cast fails, things explode...but I want it to, because this shouldn't fail
			// unless someone redefines LP's ObjectScope enum to `long` (boxed types aren't coerced)
			scope = (ObjectScope)(int)scopeArgumentValue.Value;
			return true;
		}

		/// <summary>
		/// Gets the `T` in a factory of type `IFactory<out T>` that is assignable to `baseType`.
		/// </summary>
		/// <param name="semanticModel"></param>
		/// <param name="baseType">An interface or base class that `T` is assignable to, or T itself.</param>
		/// <param name="factoryType">IFactory<out T></param>
		/// <returns></returns>
		protected ITypeSymbol GetConstructedTypeOfIFactory(
			SemanticModel semanticModel,
			ITypeSymbol baseType,
			ITypeSymbol factoryType
		) {
			// get IFactory<Foo> from FooFactory : IFactory<Foo>, IFactory<OtherFoo>
			var implementedIFactoryInterface = GetImplementedIFactoryInterface( factoryType, baseType, semanticModel.Compilation );
			if( implementedIFactoryInterface == null ) {
				return null;
			}

			// bail if no type argument (in the middle of typing)
			if( implementedIFactoryInterface.TypeArguments.Length == 0 ) {
				return null;
			}

			// get T from IFactory<T>
			return implementedIFactoryInterface.TypeArguments[0];
		}

		private static INamedTypeSymbol GetImplementedIFactoryInterface( 
			ITypeSymbol factoryType,
			ITypeSymbol baseType,
			Compilation compilation 
		) {
			var iFactoryType = compilation.GetTypeByMetadataName( IFactoryTypeMetadataName );

			var factoryInterfacesImplemented = factoryType.AllInterfaces
				.Where( i => i.ConstructedFrom == iFactoryType );

			foreach( var iface in factoryInterfacesImplemented ) {
				var t = iface.TypeArguments[0];

				if( t == baseType ) {
					return iface;
				}

				var conversionToBaseType = compilation.ClassifyConversion( source: t, destination: baseType );
				if( conversionToBaseType.IsImplicit ) {
					return iface;
				}
			}

			return null;
		}
	}
}
