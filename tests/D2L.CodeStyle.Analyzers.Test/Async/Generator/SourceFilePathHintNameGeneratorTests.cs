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

		CollectionAssert.AreEqual(
			ImmutableArray.Create(
				"bar.g"
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

		CollectionAssert.AreEqual(
			ImmutableArray.Create(
				"bar.g",
				"baz.g"
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

		CollectionAssert.AreEqual(
			ImmutableArray.Create(
				"sub_bar.g",
				"sub_baz.g",
				"quux.g"
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

		CollectionAssert.AreEqual(
			ImmutableArray.Create(
				"sub_bar.g",
				"sub_baz.g",
				"sub_bar0.g"
			),
			gen.GetHintNames()
		);
	}

	[Test]
	public void Ugly() {
		var gen = SourceFilePathHintNameGenerator.Create(
			ImmutableArray.Create(
				@"C:\foo\bar\abc.cs",
				@"D:\foo\bar\baz\def.cs"
			)
		);

		CollectionAssert.AreEqual(
			ImmutableArray.Create(
				@"C_foo_bar_abc.g",
				@"D_foo_bar_baz_def.g"
			),
			gen.GetHintNames()
		);
	}

	[Test]
	public void UnixStylePaths() {
		var gen = SourceFilePathHintNameGenerator.Create(
			ImmutableArray.Create(
				"foo/bar/abc.cs",
				"foo/def.cs",
				"foo/bar/baz/ghi.cs"
			)
		);

		CollectionAssert.AreEqual(
			ImmutableArray.Create(
				"bar_abc.g",
				"def.g",
				"bar_baz_ghi.g"
			),
			gen.GetHintNames()
		);
	}
}
