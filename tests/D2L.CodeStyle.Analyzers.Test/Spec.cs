using D2L.CodeStyle.SpecTests;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {

	[TestFixtureSource( typeof( SpecTestsProvider ), nameof( SpecTestsProvider.GetAll ) )]
	internal sealed class Spec : SpecTestFixtureBase {

		public Spec( SpecTest test ) : base( test ) { }
	}
}
