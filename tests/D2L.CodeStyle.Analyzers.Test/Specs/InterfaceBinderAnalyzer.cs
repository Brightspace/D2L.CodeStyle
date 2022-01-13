// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DataRecordConverters.InterfaceBinderAnalyzer

using System;
using D2L.LP.Serialization;

namespace D2L.LP.LayeredArch.Data {

	public static class InterfaceBinder<T> { }
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
			var type = typeof( InterfaceBinder< /* InterfaceBinder_InterfacesOnly(ClassDto) */ ClassDto /**/ > );
		}

		public void UsedWithInterface() {
			var type = typeof( InterfaceBinder<InterfaceDto> );
		}

		public void GenericTypeDefinition() {
			var type = typeof( InterfaceBinder<> );
		}

		public void UnknownGenericTypeArgument() {
			var type = typeof( InterfaceBinder< /* InterfaceBinder_InterfacesOnly(Wacky) */ Wacky /**/ > );
		}
	}

	public class Unrelated {

		public void UnknownGenericTypeDefinition() {
			var type = typeof( Wacky<int> );
		}

		public void NotInterfaceBinder() {
			var type = typeof( Action<int> );
		}

		public void TooManyGenericTypeArguments() {
			var type = typeof( Action<int, int> );
		}
	}
}
