using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Common.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Common {
	[TestFixture]
	public class KnownImmutableTypesTests {

		[Test]
		public void IsTypeKnownImmutable_DefaultlyImmutable_True() {
			var knownTypes = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );
			var type = Field( "System.Version foo" ).Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.True( result );
		}

		[Test]
		public void IsTypeKnownImmutable_DefaultlyNotImmutable_False() {
			var knownTypes = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );
			var type = Field( "System.IDisposable foo" ).Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.False( result );
		}

		[Test]
		public void IsTypeKnownImmutable_DeclaredImmutable_True() {
			var knownTypes = new KnownImmutableTypes( new HashSet<string> {
				"System.IDisposable"
			}.ToImmutableHashSet() );
			var type = Field( "System.IDisposable foo" ).Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.True( result );
		}

		[Test]
		public void IsTypeKnownImmutable_NotDeclaredImmutableAndNotDefault_False() {
			var knownTypes = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );
			var type = Field( "System.IDisposable foo" ).Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.False( result );
		}

	}
}
