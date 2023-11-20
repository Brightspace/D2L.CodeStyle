// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DataRecordConverters.InterfaceBinderAnalyzer

using System;
using D2L.LP.Serialization;

namespace D2L.LP.LayeredArch.Data {

	public static class InterfaceBinder<T> {

		private void PrivateMethod() {
			// This would be an error if the analyzer didn't only consider the public API
			PrivateMethod2();
		}
		private void PrivateMethod2() { }

		public static object AllFields { get; } = null;
		public static object CreateAllFields() => null;

		public static object MatchedFields { get; } = null;
		public static object CreateMathedFields() => null;

	}
}

namespace SpecTests {

	using D2L.LP.LayeredArch.Data;

	public class ClassDto {
		int X { get; set; }
	}

	public interface InterfaceDto {
		int X { get; }
	}

	public class TestCases {

		public void UsedWithClass() {
			// This used to be considered an error; however, the updated version of the analyzer
			// only looks at member accesses rather than generic type names to be less expensive
			_ = typeof( InterfaceBinder<ClassDto> );

			_ = /* InterfaceBinder_InterfacesOnly(ClassDto) */ InterfaceBinder<ClassDto>.AllFields /**/;
			_ = /* InterfaceBinder_InterfacesOnly(ClassDto) */ InterfaceBinder<ClassDto>.CreateAllFields() /**/;
			_ = /* InterfaceBinder_InterfacesOnly(ClassDto) */ InterfaceBinder<ClassDto>.MatchedFields /**/;
			_ = /* InterfaceBinder_InterfacesOnly(ClassDto) */ InterfaceBinder<ClassDto>.CreateMathedFields() /**/;
		}

		public void UsedWithInterface() {
			_ = typeof( InterfaceBinder<InterfaceDto> );

			_ = InterfaceBinder<InterfaceDto>.AllFields;
			_ = InterfaceBinder<InterfaceDto>.CreateAllFields;
			_ = InterfaceBinder<InterfaceDto>.MatchedFields;
			_ = InterfaceBinder<InterfaceDto>.CreateMathedFields;
		}

		public void GenericTypeDefinition() {
			_ = typeof( InterfaceBinder<> );
		}

		public void UnknownGenericTypeArgument() {
			_ = typeof( InterfaceBinder<Wacky> );

			_ = /* InterfaceBinder_InterfacesOnly(Wacky) */ InterfaceBinder<Wacky>.AllFields /**/;
			_ = /* InterfaceBinder_InterfacesOnly(Wacky) */ InterfaceBinder<Wacky>.CreateAllFields() /**/;
			_ = /* InterfaceBinder_InterfacesOnly(Wacky) */ InterfaceBinder<Wacky>.MatchedFields /**/;
			_ = /* InterfaceBinder_InterfacesOnly(Wacky) */ InterfaceBinder<Wacky>.CreateMathedFields() /**/;
		}
	}
}
