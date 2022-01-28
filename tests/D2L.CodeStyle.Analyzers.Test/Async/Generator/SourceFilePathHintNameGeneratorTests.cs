using System.Collections.Immutable;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Async.Generator;

[TestFixture]
internal class SourceFilePathHintNameGeneratorTests {
	[Test]
	public void Empty() {
		var gen = SourceFilePathHintNameGenerator.Create(
			ImmutableArray<string>.Empty
		);

		CollectionAssert.IsEmpty( gen.GetHintNames() );
	}

	[Test]
	public void Single() {
		var gen = SourceFilePathHintNameGenerator.Create(
			ImmutableArray.Create(
				@"C:\foo\bar.cs"
			)
		);

		CollectionAssert.AreEquivalent(
			ImmutableArray.Create(
				"bar"
			),
			gen.GetHintNames()
		);
	}

	[Test]
	public void Two() {
		var gen = SourceFilePathHintNameGenerator.Create(
			ImmutableArray.Create(
				@"C:\foo\bar.cs",
				@"C:\foo\baz.cs"
			)
		);

		CollectionAssert.AreEquivalent(
			ImmutableArray.Create(
				"bar",
				"baz"
			),
			gen.GetHintNames()
		);
	}

	[Test]
	public void Three() {
		var gen = SourceFilePathHintNameGenerator.Create(
			ImmutableArray.Create(
				//subdir
				@"C:\foo\sub\bar.cs",
				@"C:\foo\sub\baz.cs",

				@"C:\foo\quux.cs"
			)
		);

		CollectionAssert.AreEquivalent(
			ImmutableArray.Create(
				"sub_bar",
				"sub_baz",
				"quux"
			),
			gen.GetHintNames()
		);
	}

	[Test]
	public void ThreeAmbiguous() {
		var gen = SourceFilePathHintNameGenerator.Create(
			ImmutableArray.Create(
				//subdir
				@"C:\foo\sub\bar.cs",
				@"C:\foo\sub\baz.cs",

				@"C:\foo\sub_bar.cs"
			)
		);

		CollectionAssert.AreEquivalent(
			ImmutableArray.Create(
				"sub_bar",
				"sub_baz",
				"sub_bar0"
			),
			gen.GetHintNames()
		);
	}
}
