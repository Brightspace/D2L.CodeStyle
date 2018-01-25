using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DependencyInjection.Domain {
	// 	public static void RegisterPlugin<TExtensionPoint, TDependencyType, TConcreteType>(
	//			this IDependencyRegistry @this,
	//			ObjectScope scope
	//		)
	//		where TConcreteType : TDependencyType
	//		where TExtensionPoint : IExtensionPoint<TDependencyType>;
	//	public static void RegisterPluginFactory<TExtensionPoint, TDependencyType, TFactoryType>(
	//			this IDependencyRegistry @this,
	//			ObjectScope scope
	//		)
	//		where TFactoryType : IFactory<TDependencyType>
	//		where TExtensionPoint : IExtensionPoint<TDependencyType>;
	internal sealed class RegisterPluginForExtensionPointExpression : DependencyRegistrationExpression {
		internal override bool CanHandleMethod( IMethodSymbol method ) {
			return ( method.Name == "RegisterPlugin" || method.Name == "RegisterPluginFactory" )
				&& method.IsExtensionMethod
				&& method.Parameters.Length == 1
				&& method.TypeArguments.Length == 3;
		}

		internal override DependencyRegistration GetRegistration( IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel ) {
			if( arguments.Count != 1 ) {
				return null;
			}

			ObjectScope scope;
			if( !TryGetObjectScope( arguments[0], semanticModel, out scope ) ) {
				return null;
			}

			if( method.Name.Contains( "Factory" ) ) {

				ITypeSymbol concreteType = GetConstructedTypeOfIFactory(
					semanticModel,
					method.TypeArguments[0],
					method.TypeArguments[1]
				);

				return DependencyRegistration.Factory( 
					scope, 
					method.TypeArguments[1], 
					method.TypeArguments[2],
					concreteType
				);
			} else {
				return DependencyRegistration.NonFactory( 
					scope, 
					method.TypeArguments[1], 
					method.TypeArguments[2] 
				);
			}
		}
	}
}
