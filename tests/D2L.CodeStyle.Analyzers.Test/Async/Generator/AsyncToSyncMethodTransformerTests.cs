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
		var actual = Transform( @"[GenerateSync] async Task HelloAsync( int abc, string def ) { await Task.Delay( 3 ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Hello( int abc, string def ) {throw null;}", actual.Value.ToFullString() );
	}

	[Test]
	public void BasicTaskT() {
		var actual = Transform( @"[GenerateSync] async Task<int> HelloAsync() { await Task.Delay( 3 ); return 4; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] int Hello() {throw null;}", actual.Value.ToFullString() );
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
			compilation.GetSemanticModel( methodDecl.SyntaxTree )
		);

		return transformer.Transform( methodDecl );
	}

	public static (Compilation, MethodDeclarationSyntax) ParseMethod( string methodSource ) {
		var wrappedAndParsed = CSharpSyntaxTree.ParseText( @$"
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

class TestType{{{methodSource}}}" );

		var methodDecl = wrappedAndParsed.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

		var compilation = CSharpCompilation.Create(
			assemblyName: "test",
			references: References.Value,
			syntaxTrees: new[] { wrappedAndParsed },
			options: new(
				// Not actually going to write the output
				outputKind: OutputKind.DynamicallyLinkedLibrary,
				warningLevel: 0,
				optimizationLevel: OptimizationLevel.Debug
			)
		);

		return (compilation, methodDecl);
	}

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
