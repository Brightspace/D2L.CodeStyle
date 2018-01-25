using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {
	[TestFixture]
	internal sealed class DiagnosticsTests {

		[Test]
		public void DiagnosticIDsMustBeUnique() {
			var diagnostics = typeof( Diagnostics ).GetFields(
				BindingFlags.Public | BindingFlags.Static
			).Select( field => field.GetValue( null ) )
			.Cast<DiagnosticDescriptor>();

			var ids = diagnostics.Select( d => d.Id ).ToArray();

			CollectionAssert.AllItemsAreUnique( ids, "Found duplicate diagnostic ID." );
		}

	}
}
