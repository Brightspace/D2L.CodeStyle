using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Async.Generator;
using D2L.CodeStyle.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Async.Generator.SyntaxTransformer;

namespace D2L.CodeStyle.Analyzers.Tests.Async.Generator;

[TestFixture]
internal sealed class AsyncToSyncMethodTransformerTests {
	[Test]
	public void BasicTask() {
		var actual = Transform( @"[GenerateSync] async Task HelloAsync( int abc, string def ) { return; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Hello( int abc, string def ) { return; }", actual.Value.ToFullString() );
	}

	[Test]
	public void BasicTaskT() {
		var actual = Transform( @"[GenerateSync] async Task<int> HelloAsync() { return 4; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] int Hello() { return 4; }", actual.Value.ToFullString() );
	}

	[Test]
	public void BasicTaskArrow() {
		var actual = Transform( @"[GenerateSync] async Task<int> HelloAsync() => 4;" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] int Hello() => 4;", actual.Value.ToFullString() );
	}

	[Test]
	public void Await() {
		var actual = Transform( @"[GenerateSync] async Task<int> HelloAsync() { return await q; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] int Hello() { return q; }", actual.Value.ToFullString() );
	}

	[Test]
	public void AwaitIdentifier() {
		var actual = Transform( @"[GenerateSync] async Task HelloAsync() { await q; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Hello() { ; }", actual.Value.ToFullString() );
	}

	[Test]
		public void Silly() {
		var actual = Transform( @"[GenerateSync]
async Task<int> HelloAsync() {
	if( (await q) == (7 - (await q))*2 ) {
		return sizeof( int );
	} else if( this[0]++ == ++this[await q] ) {
		throw new NotImplementedException( await q );
		throw new X{ Y = await q };
	} else return await q;

	{
		{
			{ ;;; q; }
		}
	}

	goto lol;

	do {
		while( await q ) {
			continue;
			lol: break;
		}
	} while( await q );

	this = await q;

	await q;

	return 123;
}" );

		var expected = @"[Blocking]
int Hello() {
	if( (q) == (7 - (q))*2 ) {
		return sizeof( int );
	} else if( this[0]++ == ++this[q] ) {
		throw new NotImplementedException( q );
		throw new X{ Y = q };
	} else return q;

	{
		{
			{ ;;; q; }
		}
	}

	goto lol;

	do {
		while( q ) {
			continue;
			lol: break;
		}
	} while( q );

	this = q;

	;

	return 123;
}";

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( expected, actual.Value.ToFullString() );
	}


	[Test]
	public void GenerateSyncOnNonAsyncThingFails() {
		var actual = Transform( @"[GenerateSync] void Hello() {}" );

		Assert.IsFalse( actual.Success );

		AssertDiagnostics(
			actual.Diagnostics,
			// expected:
			Diagnostics.ExpectedAsyncSuffix,
			Diagnostics.NonTaskReturnType
		);
	}

	// loosly assert that the right sorts of diagnostics came out
	private static void AssertDiagnostics( IEnumerable<Diagnostic> actual, params DiagnosticDescriptor[] expected ) {
		CollectionAssert.AreEquivalent(
			actual.Select( d => d.Descriptor ),
			expected
		);
	}

	public static TransformResult<MethodDeclarationSyntax> Transform( string methodSource ) {
		var (compilation, methodDecl) = ParseMethod( methodSource );

		var transformer = new AsyncToSyncMethodTransformer(
			compilation.GetSemanticModel( methodDecl.SyntaxTree ),
			CancellationToken.None
		);

		return transformer.Transform( methodDecl );
	}

	public static (Compilation, MethodDeclarationSyntax) ParseMethod( string methodSource ) {
		var wrappedAndParsed = CSharpSyntaxTree.ParseText( @$"
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

class TestType{{{methodSource}}}" );

		var methodDecl = wrappedAndParsed.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

		var compilation = CreateSyncGeneratorTestCompilation( wrappedAndParsed );

		return (compilation, methodDecl);
	}

	internal static Compilation CreateSyncGeneratorTestCompilation( params SyntaxTree[] trees )
		=> CSharpCompilation.Create(
		assemblyName: "test",
		references: References.Value,
		syntaxTrees: trees,
		options: new(
			// Not actually going to write the output
			outputKind: OutputKind.DynamicallyLinkedLibrary,
			warningLevel: 0,
			optimizationLevel: OptimizationLevel.Debug
		)
	);

	private static readonly Lazy<ImmutableArray<MetadataReference>> References = new(
		() => new[] {

			typeof( object ),
			typeof( Task ),
			typeof( GenerateSyncAttribute )

		}.Select( t => t.Assembly.Location )
		.Distinct()
		.Select( l => (MetadataReference)MetadataReference.CreateFromFile( l ) )
		.ToImmutableArray()
	);
}
