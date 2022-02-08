// analyzer: D2L.CodeStyle.Analyzers.Language.OnlyVisibleToAnalyzer

using System;

using D2L.CodeStyle.Annotations.Contract;
using static Helpers;
using Targets;

namespace D2L.CodeStyle.Annotations.Contract {

	[AttributeUsage(
		validOn: AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = false
	)]
	public sealed class OnlyVisibleToTypeAttribute : Attribute {
		public OnlyVisibleToTypeAttribute( Type type ) { }
		public OnlyVisibleToTypeAttribute( string fullyQualifiedTypeName, string assemblyName ) { }
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

			InstanceMembers instance = null;
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

			InstanceMembers instance = null;
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

			InstanceMembers instance = null;
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

			InstanceMembers instance = null;
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
	}
}

namespace TestCases {

	public static class AllowedGenericCaller<T> {
		public static void Test() {
			GenericCallerTypes.VisibleByTypeOf();
			GenericCallerTypes.VisibleByQualifiedTypeName();
		}
	}

	public static class DisallowedGenericCaller<T> {
		public static void Test() {
			/* MemberNotVisibleToCaller(VisibleByTypeOf) */ GenericCallerTypes.VisibleByTypeOf() /**/;
			/* MemberNotVisibleToCaller(VisibleByQualifiedTypeName) */ GenericCallerTypes.VisibleByQualifiedTypeName() /**/;
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
