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
	public sealed class OnlyVisibleToAttribute : Attribute {

		public OnlyVisibleToAttribute( string fullyQualifiedMetadataName ) {
			FullyQualifiedMetadataName = fullyQualifiedMetadataName;
		}

		public string FullyQualifiedMetadataName { get; }
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

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		void RestrictedMethod();

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		int RestrictedPropertyGetter { get; }

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		int RestrictedPropertySetter { set; }
	}

	public sealed class InstanceMembers {

		public void UnrestrictedMethod() { }
		public int UnrestrictedPropertyGetter { get { return 1; } }
		public int UnrestrictedPropertySetter { set { } }

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		public void RestrictedMethod() { }

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		public int RestrictedPropertyGetter { get { return 1; } }

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		public int RestrictedPropertySetter { set { } }
	}

	public static class StaticMembers {

		public static void UnrestrictedMethod() { }
		public static int UnrestrictedPropertyGetter { get { return 1; } }
		public static int UnrestrictedPropertySetter { set { } }

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		public static void RestrictedMethod() { }

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
		public static int RestrictedPropertyGetter { get { return 1; } }

		[OnlyVisibleTo( TestCases.AllowedCaller.MetadataName )]
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

		[OnlyVisibleTo( TestCases.AllowedCallerA.MetadataName )]
		[OnlyVisibleTo( TestCases.AllowedCallerB.MetadataName )]
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

		[OnlyVisibleTo( "Unknown.Type" )]
		public static void Action() { }
	}
}

namespace TestCases {

	public static partial class DisallowedCaller {

		public static void ResitctedMemberToUnkonwnCaller() {
			/* MemberNotVisibleToCaller(Action) */ UnknownTargetTypes.Action() /**/;
		}
	}
}
