using D2L.CodeStyle.Analyzers.Async.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Tests.Async.Generator;

/// <summary>
/// Integration tests for the generator as a whole.
/// Prefer unit tests of AsyncToSyncTransformer and FileCollector as they are
/// simpler. We just want a bit of smoke-testing here.
/// </summary>
[TestFixture]
internal sealed class SyncGeneratorTests {
	[Test]
	public void EmptyAssembly() {
		var result = RunGenerator( sources: Array.Empty<string>() );

		// We shouldn't have crashed anyway

		AssertNoNewTrees( result );
	}

	[Test]
	public void OneFileNoMethods() {
		var result = RunGenerator( @"
using System;

namespace Foo;

sealed class Bar {
	public void Baz() => Console.WriteLine( ""hello"" );
	public int Add( int x, int y ) => x + y;
}"
		);

		AssertNoNewTrees( result );
	}

	[Test]
	public void OneFileWithOneMethod() {
		var result = RunGenerator( @"
using System;
using System.IO;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

namespace Foo;

sealed class Bar {
	public void Baz() => Console.WriteLine( ""hello"" );

	[GenerateSync]
	public async Task BazAsync( StreamWriter x ) { await x.WriteAsync( ""hello"" ); }

	public int Add( int x, int y ) => x + y;
}"
		);

		AssertNewTrees( result, @"
using System;
using System.IO;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

namespace Foo;

partial class Bar {

	[Blocking]
	public void Baz( StreamWriter x ) {throw null;}
}"
		);
	}

	[Test]
	public void Complicated() {
		var result = RunGenerator( @"
using System;
using System.IO;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

namespace Foo;

sealed partial class Bar {
	public void Baz() => Console.WriteLine( ""hello"" );

	[GenerateSync]
	public async Task BazAsync( StreamWriter x ) { await x.WriteAsync( ""hello"" ); }

	public int Add( int x, int y ) => x + y;
}", @"
using System;
using System.IO;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

namespace Foo;

public sealed partial class Abcdefg {
	[GenerateSync]
	public async Task<int> FooAsync() { await Task.Delay( 3 ); return 7; }

	[GenerateSync]
	public async Task BarAsync() { await FooAsync(); }
}"
		);

		AssertNewTrees( result, @"
using System;
using System.IO;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

namespace Foo;

partial class Bar {

	[Blocking]
	public void Baz( StreamWriter x ) {throw null;}
}", @"
using System;
using System.IO;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

namespace Foo;

partial class Abcdefg {
	[Blocking]
	public int Foo() {throw null;}

	[Blocking]
	public void Bar() {throw null;}
}"
		);
	}

	public (Compilation Before, Compilation After) RunGenerator( params string[] sources ) {
		var oldCompilation = AsyncToSyncMethodTransformerTests.CreateSyncGeneratorTestCompilation(
			sources.Select( src => CSharpSyntaxTree.ParseText( src ) ).ToArray()
		);

		var driver = CSharpGeneratorDriver.Create( new SyncGenerator() );

		driver.RunGeneratorsAndUpdateCompilation(
			oldCompilation,
			out var newCompilation,
			out var diagnostics
		);

		// Test diagnostics in unit tests not integration tests.
		CollectionAssert.IsEmpty( diagnostics );

		return (oldCompilation, newCompilation);
	}

	public static void AssertNewTrees(
		(Compilation Before, Compilation After) result,
		params string[] expected
	) {
		var actual = result.After.SyntaxTrees
			// Filter out source syntax
			.Except( result.Before.SyntaxTrees )
			// Serialize to string to ignore some metadata. Our generator
			// is only responsible for giving a string in the first place along
			// with a hint name which must be unique. If we failed to do the
			// unique thing in one of these tests an exception would be thrown,
			// and how that hint name is used to create a file path is an
			// implementation detail we shouldn't rely on anyway.
			.Select( t => t.ToString() );

		CollectionAssert.AreEquivalent( expected, actual );
	}

	private static void AssertNoNewTrees( (Compilation Before, Compilation After) result )
		=> AssertNewTrees( result, Array.Empty<string>() );

}
