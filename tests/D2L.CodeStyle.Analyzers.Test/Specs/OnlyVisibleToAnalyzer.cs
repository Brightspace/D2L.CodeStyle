// analyzer: D2L.CodeStyle.Analyzers.Language.OnlyVisibleToAnalyzer

using System;

using D2L.CodeStyle.Annotations.Contract;
using static Helpers;
using Targets;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		validOn: AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class OnlyVisibleToTypeAttribute : Attribute {
		public OnlyVisibleToTypeAttribute( Type type, bool inherited = true ) { }
		public OnlyVisibleToTypeAttribute( string fullyQualifiedTypeName, string assemblyName, bool inherited = true ) { }
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
		public void RestrictedMethod() { }

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
	public static class InterfaceWithInheritance {
		[OnlyVisibleToType( typeof( RestrictedClass ) )]
		[OnlyVisibleToType( typeof( InterfaceWithInheritance ) )]
		[OnlyVisibleToType( typeof( TestCases.AllowedInterfaceWithInheritanceCaller ) )]
		public interface IBaseInterface {
			string SomeProperty;
			void SomeMethod( string someValue );
		}

		public class RestrictedClass : IBaseInterface {
			public RestrictedClass() { }
			string SomeProperty => "SomeProperty";
			void SomeMethod( string someValue ) { }
		}

		public static void GenericArgumentMethod<T>() { }

		public static IBaseInterface GetInterfaceObject() {
			return new RestrictedClass();
		}

		public static RestrictedClass GetClassObject() {
			return new RestrictedClass();
		}
	}
}

namespace TestCases {
	public static class AllowedInterfaceWithInheritanceCaller {
		public static void DirectUsage() {
			InterfaceWithInheritance.IBaseInterface interfaceObject = new InterfaceWithInheritance.RestrictedClass();
			InterfaceWithInheritance.RestrictedClass classObject = new InterfaceWithInheritance.RestrictedClass();

			var implicitInterfaceObject = InterfaceWithInheritance.GetInterfaceObject();
			var implicitClassObject = InterfaceWithInheritance.GetClassObject();

			InterfaceWithInheritance.GenericArgumentMethod<InterfaceWithInheritance.IBaseInterface>();
			InterfaceWithInheritance.GenericArgumentMethod<InterfaceWithInheritance.RestrictedClass>();
		}

		public static void InterfaceParameter( InterfaceWithInheritance.IBaseInterface p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( InterfaceWithInheritance.RestrictedClass p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericInterface<T>() where T : InterfaceWithInheritance.IBaseInterface, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : InterfaceWithInheritance.RestrictedClass, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}

	public static class DisallowedInterfaceWithInheritanceCaller {
		public static void DirectUsage() {
			/* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithInheritance.IBaseInterface /**/ interfaceObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithInheritance.RestrictedClass /**/();
			/* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithInheritance.RestrictedClass /**/ classObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithInheritance.RestrictedClass /**/();

			/* TypeNotVisibleToCaller(IBaseInterface) */ var /**/ implicitInterfaceObject = InterfaceWithInheritance.GetInterfaceObject();
			/* TypeNotVisibleToCaller(RestrictedClass) */ var /**/ implicitClassObject = InterfaceWithInheritance.GetClassObject();

			InterfaceWithInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithInheritance.IBaseInterface /**/>();
			InterfaceWithInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithInheritance.RestrictedClass /**/>();
		}

		public static void InterfaceParameter( /* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithInheritance.IBaseInterface /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithInheritance.RestrictedClass /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericInterface<T>() where T : /* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithInheritance.IBaseInterface /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithInheritance.RestrictedClass /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}
}

// ===============================================================================

namespace Targets {
	public static class InterfaceWithoutInheritance {
		[OnlyVisibleToType( typeof( RestrictedClass ) )]
		[OnlyVisibleToType( typeof( InterfaceWithoutInheritance ) )]
		[OnlyVisibleToType( typeof( TestCases.AllowedInterfaceWithoutInheritanceCaller ) )]
		public interface IBaseInterface {
			string SomeProperty;
			void SomeMethod( string someValue );
		}

		public class RestrictedClass : IBaseInterface {
			public RestrictedClass() { }
			string SomeProperty => "SomeProperty";
			void SomeMethod( string someValue ) { }
		}

		public static void GenericArgumentMethod<T>() { }

		public static IBaseInterface GetInterfaceObject() {
			return new RestrictedClass();
		}

		public static RestrictedClass GetClassObject() {
			return new RestrictedClass();
		}
	}
}

namespace TestCases {
	public static class AllowedInterfaceWithoutInheritanceCaller {
		public static void DirectUsage() {
			InterfaceWithoutInheritance.IBaseInterface interfaceObject = new InterfaceWithoutInheritance.RestrictedClass();
			InterfaceWithoutInheritance.RestrictedClass classObject = new InterfaceWithoutInheritance.RestrictedClass();

			var implicitInterfaceObject = InterfaceWithoutInheritance.GetInterfaceObject();
			var implicitClassObject = InterfaceWithoutInheritance.GetClassObject();

			InterfaceWithoutInheritance.GenericArgumentMethod<InterfaceWithoutInheritance.IBaseInterface>();
			InterfaceWithoutInheritance.GenericArgumentMethod<InterfaceWithoutInheritance.RestrictedClass>();
		}

		public static void InterfaceParameter( InterfaceWithoutInheritance.IBaseInterface p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( InterfaceWithoutInheritance.RestrictedClass p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericInterface<T>() where T : InterfaceWithoutInheritance.IBaseInterface, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : InterfaceWithoutInheritance.RestrictedClass, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}

	public static class DisallowedInterfaceWithoutInheritanceCaller {
		public static void DirectUsage() {
			/* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithoutInheritance.IBaseInterface /**/ interfaceObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithoutInheritance.RestrictedClass /**/();
			/* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithoutInheritance.RestrictedClass /**/ classObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithoutInheritance.RestrictedClass /**/();

			/* TypeNotVisibleToCaller(IBaseInterface) */ var /**/ implicitInterfaceObject = InterfaceWithoutInheritance.GetInterfaceObject();
			/* TypeNotVisibleToCaller(RestrictedClass) */ var /**/ implicitClassObject = InterfaceWithoutInheritance.GetClassObject();

			InterfaceWithoutInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithoutInheritance.IBaseInterface /**/>();
			InterfaceWithoutInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithoutInheritance.RestrictedClass /**/>();
		}

		public static void InterfaceParameter( /* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithoutInheritance.IBaseInterface /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithoutInheritance.RestrictedClass /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericInterface<T>() where T : /* TypeNotVisibleToCaller(IBaseInterface) */ InterfaceWithoutInheritance.IBaseInterface /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : /* TypeNotVisibleToCaller(RestrictedClass) */ InterfaceWithoutInheritance.RestrictedClass /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}
}


// ===============================================================================

namespace Targets {
	public static class ClassWithInheritance {
		[OnlyVisibleToType( typeof( RestrictedClass ) )]
		[OnlyVisibleToType( typeof( ClassWithInheritance ) )]
		[OnlyVisibleToType( typeof( TestCases.AllowedClassWithInheritanceCaller ) )]
		public abstract class BaseClass {
			string SomeProperty;
			void SomeMethod( string someValue );
		}

		public class RestrictedClass : BaseClass {
			public RestrictedClass() { }
			string SomeProperty => "SomeProperty";
			void SomeMethod( string someValue ) { }
		}

		public static void GenericArgumentMethod<T>() { }

		public static BaseClass GetBaseObject() {
			return new RestrictedClass();
		}

		public static RestrictedClass GetClassObject() {
			return new RestrictedClass();
		}
	}
}

namespace TestCases {
	public static class AllowedClassWithInheritanceCaller {
		public static void DirectUsage() {
			ClassWithInheritance.BaseClass interfaceObject = new ClassWithInheritance.RestrictedClass();
			ClassWithInheritance.RestrictedClass classObject = new ClassWithInheritance.RestrictedClass();

			var implicitBaseObject = ClassWithInheritance.GetBaseObject();
			var implicitClassObject = ClassWithInheritance.GetClassObject();

			ClassWithInheritance.GenericArgumentMethod<ClassWithInheritance.BaseClass>();
			ClassWithInheritance.GenericArgumentMethod<ClassWithInheritance.RestrictedClass>();
		}

		public static void BaseParameter( ClassWithInheritance.BaseClass p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( ClassWithInheritance.RestrictedClass p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericBase<T>() where T : ClassWithInheritance.BaseClass, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : ClassWithInheritance.RestrictedClass, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}

	public static class DisallowedClassWithInheritanceCaller {
		public static void DirectUsage() {
			/* TypeNotVisibleToCaller(BaseClass) */ ClassWithInheritance.BaseClass /**/ interfaceObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithInheritance.RestrictedClass /**/();
			/* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithInheritance.RestrictedClass /**/ classObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithInheritance.RestrictedClass /**/();

			/* TypeNotVisibleToCaller(BaseClass) */ var /**/ implicitBaseObject = ClassWithInheritance.GetBaseObject();
			/* TypeNotVisibleToCaller(RestrictedClass) */ var /**/ implicitClassObject = ClassWithInheritance.GetClassObject();

			ClassWithInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(BaseClass) */ ClassWithInheritance.BaseClass /**/>();
			ClassWithInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithInheritance.RestrictedClass /**/>();
		}

		public static void BaseParameter( /* TypeNotVisibleToCaller(BaseClass) */ ClassWithInheritance.BaseClass /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithInheritance.RestrictedClass /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericBase<T>() where T : /* TypeNotVisibleToCaller(BaseClass) */ ClassWithInheritance.BaseClass /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithInheritance.RestrictedClass /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}
}

// ===============================================================================

namespace Targets {
	public static class ClassWithoutInheritance {
		[OnlyVisibleToType( typeof( RestrictedClass ) )]
		[OnlyVisibleToType( typeof( ClassWithoutInheritance ) )]
		[OnlyVisibleToType( typeof( TestCases.AllowedClassWithoutInheritanceCaller ) )]
		public interface BaseClass {
			string SomeProperty;
			void SomeMethod( string someValue );
		}

		public class RestrictedClass : BaseClass {
			public RestrictedClass() { }
			string SomeProperty => "SomeProperty";
			void SomeMethod( string someValue ) { }
		}

		public static void GenericArgumentMethod<T>() { }

		public static BaseClass GetBaseObject() {
			return new RestrictedClass();
		}

		public static RestrictedClass GetClassObject() {
			return new RestrictedClass();
		}
	}
}

namespace TestCases {
	public static class AllowedClassWithoutInheritanceCaller {
		public static void DirectUsage() {
			ClassWithoutInheritance.BaseClass interfaceObject = new ClassWithoutInheritance.RestrictedClass();
			ClassWithoutInheritance.RestrictedClass classObject = new ClassWithoutInheritance.RestrictedClass();

			var implicitBaseObject = ClassWithoutInheritance.GetBaseObject();
			var implicitClassObject = ClassWithoutInheritance.GetClassObject();

			ClassWithoutInheritance.GenericArgumentMethod<ClassWithoutInheritance.BaseClass>();
			ClassWithoutInheritance.GenericArgumentMethod<ClassWithoutInheritance.RestrictedClass>();
		}

		public static void BaseParameter( ClassWithoutInheritance.BaseClass p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( ClassWithoutInheritance.RestrictedClass p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericBase<T>() where T : ClassWithoutInheritance.BaseClass, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : ClassWithoutInheritance.RestrictedClass, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}

	public static class DisallowedClassWithoutInheritanceCaller {
		public static void DirectUsage() {
			/* TypeNotVisibleToCaller(BaseClass) */ ClassWithoutInheritance.BaseClass /**/ interfaceObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithoutInheritance.RestrictedClass /**/();
			/* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithoutInheritance.RestrictedClass /**/ classObject = new /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithoutInheritance.RestrictedClass /**/();

			/* TypeNotVisibleToCaller(BaseClass) */ var /**/ implicitBaseObject = ClassWithoutInheritance.GetBaseObject();
			/* TypeNotVisibleToCaller(RestrictedClass) */ var /**/ implicitClassObject = ClassWithoutInheritance.GetClassObject();

			ClassWithoutInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(BaseClass) */ ClassWithoutInheritance.BaseClass /**/>();
			ClassWithoutInheritance.GenericArgumentMethod</* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithoutInheritance.RestrictedClass /**/>();
		}

		public static void BaseParameter( /* TypeNotVisibleToCaller(BaseClass) */ ClassWithoutInheritance.BaseClass /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}
		public static void ClassParameter( /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithoutInheritance.RestrictedClass /**/ p ) {
			p.SomeMethod( p.SomeProperty );
		}

		public static void GenericBase<T>() where T : /* TypeNotVisibleToCaller(BaseClass) */ ClassWithoutInheritance.BaseClass /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
		public static void GenericClass<T>() where T : /* TypeNotVisibleToCaller(RestrictedClass) */ ClassWithoutInheritance.RestrictedClass /**/, new() {
			T genericObject = new T();
			genericObject.SomeMethod( p.SomeProperty );
		}
	}
}
