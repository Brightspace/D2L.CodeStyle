// analyzer: D2L.CodeStyle.Analyzers.Language.OnlyVisibleToAnalyzer

using System;

using D2L.CodeStyle.Annotations.Contract;
using static Helpers;
using Targets;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		validOn: AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class OnlyVisibleToTypeAttribute : Attribute {
		public OnlyVisibleToTypeAttribute( Type type ) { }
		public OnlyVisibleToTypeAttribute( string fullyQualifiedTypeName, string assemblyName ) { }
	}

	[AttributeUsage(
		validOn: AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class ReleaseVisibilityConstraints : Attribute {
		public ReleaseVisibilityConstraints( Type type ) { }
		public ReleaseVisibilityConstraints( string fullyQualifiedTypeName, string assemblyName ) { }
	}
}

public static class Helpers {
	public static void GetProperty( int value ) { }
}

// ===============================================================================

namespace Targets {

	public interface InterfaceMembers {

		void UnrestrictedMethod();
		int UnrestrictedPropertyGetter { get; }
		int UnrestrictedPropertySetter { set; }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		void RestrictedMethod();

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		int RestrictedPropertyGetter { get; }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		int RestrictedPropertySetter { set; }
	}

	public sealed class InstanceMembers {

		public InstanceMembers() { }
		public InstanceMembers( int value ) { }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		public InstanceMembers( int value, int value2 ) { }

		public void UnrestrictedMethod() { }
		public int UnrestrictedPropertyGetter { get { return 1; } }
		public int UnrestrictedPropertySetter { set { } }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		public void RestrictedMethod() { }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		public int RestrictedPropertyGetter { get { return 1; } }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		public int RestrictedPropertySetter { set { } }
	}

	public static class StaticMembers {

		public static void UnrestrictedMethod() { }
		public static int UnrestrictedPropertyGetter { get { return 1; } }
		public static int UnrestrictedPropertySetter { set { } }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		public static void RestrictedMethod() { }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		public static int RestrictedPropertyGetter { get { return 1; } }

		[OnlyVisibleToType( TestCases.AllowedCaller.MetadataName, "OnlyVisibleToAnalyzer" )]
		public static int RestrictedPropertySetter { set { } }
	}
}

namespace TestCases {

	public static class AllowedCaller {

		public const string MetadataName = "TestCases.AllowedCaller";

		public static void UnresitctedMembers() {

			InterfaceMembers @interface = null;
			Action interfaceMethodReference = @interface.UnrestrictedMethod;
			@interface.UnrestrictedMethod();
			GetProperty( @interface.UnrestrictedPropertyGetter );
			@interface.UnrestrictedPropertySetter = 7;

			InstanceMembers instance = new InstanceMembers();
			instance = new InstanceMembers( value: 1 );
			Action instanceMethodReference = instance.UnrestrictedMethod;
			instance.UnrestrictedMethod();
			GetProperty( instance.UnrestrictedPropertyGetter );
			instance.UnrestrictedPropertySetter = 7;

			StaticMembers.UnrestrictedMethod();
			Action staticMethodReference = StaticMembers.UnrestrictedMethod;
			GetProperty( StaticMembers.UnrestrictedPropertyGetter );
			StaticMembers.UnrestrictedPropertySetter = 7;
		}

		public static void ResitctedMembers() {

			InterfaceMembers @interface = null;
			@interface.RestrictedMethod();
			Action interfaceMethodReference = @interface.RestrictedMethod;
			GetProperty( @interface.RestrictedPropertyGetter );
			@interface.RestrictedPropertySetter = 7;

			InstanceMembers instance = new InstanceMembers( value: 1, value2: 2 );
			instance.RestrictedMethod();
			Action instanceMethodReference = instance.RestrictedMethod;
			GetProperty( instance.RestrictedPropertyGetter );
			instance.RestrictedPropertySetter = 7;

			StaticMembers.RestrictedMethod();
			Action staticMethodReference = StaticMembers.RestrictedMethod;
			GetProperty( StaticMembers.RestrictedPropertyGetter );
			StaticMembers.RestrictedPropertySetter = 7;
		}
	}

	public static partial class DisallowedCaller {

		public static void UnresitctedMembers() {

			InterfaceMembers @interface = null;
			Action interfaceMethodReference = @interface.UnrestrictedMethod;
			@interface.UnrestrictedMethod();
			GetProperty( @interface.UnrestrictedPropertyGetter );
			@interface.UnrestrictedPropertySetter = 7;

			InstanceMembers instance = new InstanceMembers();
			instance = new InstanceMembers( value: 1 );
			Action instanceMethodReference = instance.UnrestrictedMethod;
			instance.UnrestrictedMethod();
			GetProperty( instance.UnrestrictedPropertyGetter );
			instance.UnrestrictedPropertySetter = 7;

			StaticMembers.UnrestrictedMethod();
			Action staticMethodReference = StaticMembers.UnrestrictedMethod;
			GetProperty( StaticMembers.UnrestrictedPropertyGetter );
			StaticMembers.UnrestrictedPropertySetter = 7;
		}

		public static void ResitctedMembers() {

			InterfaceMembers @interface = null;
			/* MemberNotVisibleToCaller(RestrictedMethod) */ @interface.RestrictedMethod() /**/;
			Action interfaceMethodReference = /* MemberNotVisibleToCaller(RestrictedMethod) */ @interface.RestrictedMethod /**/;
			GetProperty( /* MemberNotVisibleToCaller(RestrictedPropertyGetter) */ @interface.RestrictedPropertyGetter /**/ );
			/* MemberNotVisibleToCaller(RestrictedPropertySetter) */ @interface.RestrictedPropertySetter /**/ = 7;

			InstanceMembers instance = /* MemberNotVisibleToCaller(.ctor) */ new InstanceMembers( value: 1, value2: 2 ) /**/;
			/* MemberNotVisibleToCaller(RestrictedMethod) */ instance.RestrictedMethod() /**/;
			Action instanceMethodReference = /* MemberNotVisibleToCaller(RestrictedMethod) */ instance.RestrictedMethod /**/;
			GetProperty( /* MemberNotVisibleToCaller(RestrictedPropertyGetter) */ instance.RestrictedPropertyGetter /**/ );
			/* MemberNotVisibleToCaller(RestrictedPropertySetter) */ instance.RestrictedPropertySetter /**/ = 7;

			/* MemberNotVisibleToCaller(RestrictedMethod) */ StaticMembers.RestrictedMethod() /**/;
			Action staticMethodReference = /* MemberNotVisibleToCaller(RestrictedMethod) */ StaticMembers.RestrictedMethod /**/;
			GetProperty( /* MemberNotVisibleToCaller(RestrictedPropertyGetter) */ StaticMembers.RestrictedPropertyGetter /**/ );
			/* MemberNotVisibleToCaller(RestrictedPropertySetter) */ StaticMembers.RestrictedPropertySetter /**/ = 7;
		}
	}
}

// ===============================================================================

namespace Targets {

	public static class MultipleTargets {

		[OnlyVisibleToType( TestCases.AllowedCallerA.MetadataName, "OnlyVisibleToAnalyzer" )]
		[OnlyVisibleToType( TestCases.AllowedCallerB.MetadataName, "OnlyVisibleToAnalyzer" )]
		public static void Action() { }
	}
}

namespace TestCases {

	public static class AllowedCallerA {
		public const string MetadataName = "TestCases.AllowedCallerA";
		public static void Test() {
			MultipleTargets.Action();
		}
	}

	public static class AllowedCallerB {
		public const string MetadataName = "TestCases.AllowedCallerB";
		public static void Test() {
			MultipleTargets.Action();
		}
	}
}

// ===============================================================================

namespace Targets {

	public static class UnknownTargetTypes {

		[OnlyVisibleToType( "Unknown.Type", "OnlyVisibleToAnalyzer" )]
		public static void Foo() { }

		[OnlyVisibleToType( "TestCases.DisallowedCaller", "Unknown.Assembly" )]
		public static void Bar() { }
	}
}

namespace TestCases {

	public static partial class DisallowedCaller {

		public static void ResitctedMemberToUnkonwnCaller() {
			/* MemberNotVisibleToCaller(Foo) */ UnknownTargetTypes.Foo() /**/;
			/* MemberNotVisibleToCaller(Bar) */ UnknownTargetTypes.Bar() /**/;
		}
	}
}

// ===============================================================================

namespace TestCases {

	public static class AlwaysVisibleWithinSameContainer {

		[OnlyVisibleToType( "PeterPan", "Neverland" )]
		public static void Fly() { }

		private static void InternalCaller() {
			Fly();
		}
	}

	public interface IRestrictedInterface {

		[OnlyVisibleToType( typeof( string ) )]
		void RestrictedMethod();
	}

	public class RestrictedMembersOnSelf : IRestrictedInterface {

		[OnlyVisibleToType( typeof( string ) )]
		public void RestrictedMethod() {}

		public void Caller() {

			// Ok when called on this
			this.RestrictedMethod();

			// Violation when called via interface, since not in set
			IRestrictedInterface @interface = this;
			/* MemberNotVisibleToCaller(RestrictedMethod) */ @interface.RestrictedMethod() /**/;
		}
	}
}

// ===============================================================================

namespace Targets {

	public static class TypeOfTargets {

		[OnlyVisibleToType( typeof( TestCases.AllowedTypeOfCaller ) )]
		public static void Action() { }
	}
}

namespace TestCases {

	public static class AllowedTypeOfCaller {
		public static void Test() {
			TypeOfTargets.Action();
		}
	}

	public static class DisallowedTypeOfCaller {
		public static void Test() {
			/* MemberNotVisibleToCaller(Action) */ TypeOfTargets.Action() /**/;
		}
	}
}

// ===============================================================================

namespace Targets {

	public static class GenericCallerTypes {

		[OnlyVisibleToType( typeof( TestCases.AllowedGenericCaller<> ) )]
		public static void VisibleByTypeOf() { }

		[OnlyVisibleToType( "TestCases.AllowedGenericCaller`1", "OnlyVisibleToAnalyzer" )]
		public static void VisibleByQualifiedTypeName() { }

		/// <summary>
		/// You can only make yourself visible to the generic type definition.
		/// </summary>
		[OnlyVisibleToType( typeof( TestCases.AllowedGenericCaller<int> ) )]
		public static void VisibleButWithGenericTypeArguments() { }
	}
}

namespace TestCases {

	public static class AllowedGenericCaller<T> {
		public static void Test() {
			GenericCallerTypes.VisibleByTypeOf();
			GenericCallerTypes.VisibleByQualifiedTypeName();
			/* MemberNotVisibleToCaller(VisibleButWithGenericTypeArguments) */ GenericCallerTypes.VisibleButWithGenericTypeArguments() /**/;
		}
	}

	public static class DisallowedGenericCaller<T> {
		public static void Test() {
			/* MemberNotVisibleToCaller(VisibleByTypeOf) */ GenericCallerTypes.VisibleByTypeOf() /**/;
			/* MemberNotVisibleToCaller(VisibleByQualifiedTypeName) */ GenericCallerTypes.VisibleByQualifiedTypeName() /**/;
			/* MemberNotVisibleToCaller(VisibleButWithGenericTypeArguments) */ GenericCallerTypes.VisibleButWithGenericTypeArguments() /**/;
		}
	}
}

// ===============================================================================

namespace Targets {

	public static class GenericTargetTypes<T> {

		[OnlyVisibleToType( typeof( TestCases.AllowedGenericTargetCaller ) )]
		public static void VisibleByTypeOf() { }

		[OnlyVisibleToType( "TestCases.AllowedGenericTargetCaller", "OnlyVisibleToAnalyzer" )]
		public static void VisibleByQualifiedTypeName() { }
	}
}

namespace TestCases {

	public static class AllowedGenericTargetCaller {
		public static void Test() {
			GenericTargetTypes<int>.VisibleByTypeOf();
			GenericTargetTypes<int>.VisibleByQualifiedTypeName();
		}
	}

	public static class DisallowedGenericTargetCaller {
		public static void Test() {
			/* MemberNotVisibleToCaller(VisibleByTypeOf) */ GenericTargetTypes<int>.VisibleByTypeOf() /**/;
			/* MemberNotVisibleToCaller(VisibleByQualifiedTypeName) */ GenericTargetTypes<int>.VisibleByQualifiedTypeName() /**/;
		}
	}
}

// ===============================================================================

namespace Targets {
	[OnlyVisibleToType( typeof( RestrictedWithInheritance ) )]
	[OnlyVisibleToType( typeof( RestrictedWithoutInheritance ) )]
	[OnlyVisibleToType( typeof( TestCases.AllowedInheritanceCaller ) )]
	public interface IInheritanceInterface {
		string SomeProperty;
		void SomeMethod( string someValue );
	}

	public class RestrictedWithInheritance : IInheritanceInterface {
		public RestrictedWithInheritance() { }
		string SomeProperty => "SomeProperty";
		void SomeMethod( string someValue ) { }
	}

	public class RestrictedWithDoubleInheritance : RestrictedWithInheritance {
		public RestrictedWithDoubleInheritance() { }
	}

	[ReleaseVisibilityConstraints( typeof( IInheritanceInterface ) )]
	public class RestrictedWithoutInheritance : IInheritanceInterface {
		public RestrictedWithoutInheritance() { }
		string SomeProperty => "SomeProperty";
		void SomeMethod( string someValue ) { }
	}

	public class RestrictedWithoutDoubleInheritance : RestrictedWithoutInheritance {
		public RestrictedWithoutDoubleInheritance() { }
	}
}

namespace TestCases {
	public static class AllowedInheritanceCaller {
		public static void DirectInstantiation() {
			RestrictedWithInheritance withInheritanceObject = new RestrictedWithInheritance();
			RestrictedWithDoubleInheritance withDoubleInheritanceObject = new RestrictedWithDoubleInheritance();
			RestrictedWithoutInheritance withoutInheritanceObject = new RestrictedWithoutInheritance();
			RestrictedWithoutDoubleInheritance withoutDoubleInheritanceObject = new RestrictedWithoutDoubleInheritance();
			IInheritanceInterface withInheritanceInterface = new RestrictedWithInheritance();
			IInheritanceInterface withDoubleInheritanceInterface = new RestrictedWithDoubleInheritance();
			IInheritanceInterface withoutInheritanceInterface = new RestrictedWithoutInheritance();
			IInheritanceInterface withoutDoubleInheritanceInterface = new RestrictedWithoutDoubleInheritance();
		}

		public static void InterfaceParameter( IInheritanceInterface p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithInheritanceParameter( RestrictedWithInheritance p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithDoubleInheritanceParameter( RestrictedWithDoubleInheritance p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithoutInheritanceParameter( RestrictedWithoutInheritance p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithoutDoubleInheritanceParameter( RestrictedWithoutDoubleInheritance p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericInterface<T>() where T : IInheritanceInterface, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithInheritance<T>() where T : RestrictedWithInheritance, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithDoubleInheritance<T>() where T : RestrictedWithDoubleInheritance, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithoutInheritance<T>() where T : RestrictedWithoutInheritance, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithoutDoubleInheritance<T>() where T : RestrictedWithoutDoubleInheritance, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}

	public static class DisallowedInheritanceCaller {
		public static void DirectInstantiation() {
			/* TypeNotVisibleToCaller(RestrictedWithInheritance) */ RestrictedWithInheritance /**/ withInheritanceObject = new /* TypeNotVisibleToCaller(RestrictedWithInheritance) */ RestrictedWithInheritance() /**/;
			/* TypeNotVisibleToCaller(RestrictedWithDoubleInheritance) */ RestrictedWithDoubleInheritance /**/ withDoubleInheritanceObject = new /* TypeNotVisibleToCaller(RestrictedWithDoubleInheritance) */ RestrictedWithDoubleInheritance() /**/;
			RestrictedWithoutInheritance withoutInheritanceObject = new RestrictedWithoutInheritance();
			RestrictedWithoutDoubleInheritance withoutDoubleInheritanceObject = new RestrictedWithoutDoubleInheritance();
			/* TypeNotVisibleToCaller(IInheritanceInterface) */ IInheritanceInterface /**/ withInheritanceInterface = new /* TypeNotVisibleToCaller(RestrictedWithInheritance) */ RestrictedWithInheritance() /**/;
			/* TypeNotVisibleToCaller(IInheritanceInterface) */ IInheritanceInterface /**/ withDoubleInheritanceInterface = new /* TypeNotVisibleToCaller(RestrictedWithDoubleInheritance) */ RestrictedWithDoubleInheritance() /**/;
			/* TypeNotVisibleToCaller(IInheritanceInterface) */ IInheritanceInterface /**/ withoutInheritanceInterface = new RestrictedWithoutInheritance();
			/* TypeNotVisibleToCaller(IInheritanceInterface) */ IInheritanceInterface /**/ withoutDoubleInheritanceInterface = new RestrictedWithoutDoubleInheritance();
		}

		public static void InterfaceParameter( /* TypeNotVisibleToCaller(IInheritanceInterface) */ IInheritanceInterface /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithInheritanceParameter( /* TypeNotVisibleToCaller(RestrictedWithInheritance) */ RestrictedWithInheritance /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithDoubleInheritanceParameter( /* TypeNotVisibleToCaller(RestrictedWithDoubleInheritance) */ RestrictedWithDoubleInheritance /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithoutInheritanceParameter( RestrictedWithoutInheritance p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void WithoutDoubleInheritanceParameter( RestrictedWithoutDoubleInheritance p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericInterface<T>() where T : /* TypeNotVisibleToCaller(IInheritanceInterface) */ IInheritanceInterface /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithInheritance<T>() where T : /* TypeNotVisibleToCaller(RestrictedWithInheritance) */ RestrictedWithInheritance/**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithDoubleInheritance<T>() where T : /* TypeNotVisibleToCaller(RestrictedWithDoubleInheritance) */ RestrictedWithDoubleInheritance /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithoutInheritance<T>() where T : RestrictedWithoutInheritance, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericWithoutDoubleInheritance<T>() where T : RestrictedWithoutDoubleInheritance, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}
}
