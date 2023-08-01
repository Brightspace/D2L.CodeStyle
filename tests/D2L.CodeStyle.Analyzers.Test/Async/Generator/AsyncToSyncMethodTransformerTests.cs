using System.Collections.Immutable;
using System.Security.Policy;
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
	public void Invocation() {
		var actual = Transform( @"[GenerateSync] async Task HelloAsync() { await BarAsync( Baz(), await QuuxAsync() ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Hello() { Bar( Baz(),Quux() ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void ExplicitInterfaceSpecifier() {
		var actual = Transform( @"[GenerateSync] async Task<int> Foo.BazAsync() { return intCountAsync(6); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] int Foo.Baz() { return intCount(6); }", actual.Value.ToFullString() );
	}

	[Test]
	public void VariableDeclaration() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { IBaz q = await m_bazValidator.ValidateAsync( bar ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { IBaz q = m_bazValidator.Validate( bar ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void PredefinedType() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { string.IsNullOrEmpty(""""); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { string.IsNullOrEmpty(\"\"); }", actual.Value.ToFullString() );
	}

	[Test]
	public void Cast() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { var baz = await ( Task<int> )fooHandler.ReadFooAsync( ""foo"" ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { var baz = ( int )fooHandler.ReadFoo( \"foo\" ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void ForEach() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { foreach( Task<int> fooAsync in foosAsync ) { bar += await fooAsync; } }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { foreach( int foo in foos ) { bar += foo; } }", actual.Value.ToFullString() );
	}

	[Test]
	public void ConfigureAwait() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { Foo fooFirst = m_foo.Bar( barrer ).ConfigureAwait( false ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { Foo fooFirst = m_foo.Bar( barrer ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void CompletedTaskConfigureAwaitFakeAsync() {
		var actual = Transform( @"[GenerateSync] Task BarAsync() { return Task.CompletedTask; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { return; }", actual.Value.ToFullString() );
	}

	[Test]
	public void SafeAsync() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { await m_baz.WriteLineAsync( line ).SafeAsync(); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { m_baz.WriteLine( line ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void FromResult() {
		var actual = Transform( @"[GenerateSync] Task<Baz> BarAsync() { return Task.FromResult( baz ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] Baz Bar() { return ( baz ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void FromResultGeneric() {
		var actual = Transform( @"[GenerateSync] Task<Baz> BarAsync() { return Task.FromResult<Baz>( null ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] Baz Bar() { return ( null ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void GenericNameSyntaxNonTask() {
		var actual = Transform( @"[GenerateSync] Task<Foo> BarAsync() { return FooAsync( Baz<Quux>.AllFields ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] Foo Bar() { return Foo( Baz<Quux>.AllFields ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void GenericNameSyntaxTask() {
		var actual = Transform( @"[GenerateSync] Task<Foo> BarAsync() { return FooAsync( Baz<Task<Quux>>.AllFields ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] Foo Bar() { return Foo( Baz<Quux>.AllFields ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void GenericNameSyntaxMultiple() {
		var actual = Transform( @"[GenerateSync] Task<Foo> BarAsync() { return FooAsync( Baz<Task<Quux>,Task<Fooz>>.AllFields ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] Foo Bar() { return Foo( Baz<Quux,Fooz>.AllFields ); }", actual.Value.ToFullString() );
	}

	// Not the best thing to be doing but unlikely this will happen and it should be easily caught
	[Test]
	public void DontRemoveUnreturnedFromResult() {
		var actual = Transform( @"[GenerateSync] Task BarAsync() { var Baz = Task.CompletedTask; return Baz; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { var Baz = Task.CompletedTask; return; }", actual.Value.ToFullString() );
	}

	[Test]
	public void Using() {
		var actual = Transform( @"[GenerateSync] async Task<Qux> BarAsync() { using( var foo = await m_bar.GetAsync( qux ).ConfigureAwait( false ) ) { return await( this as IBarProvider ).BarsAsync().ConfigureAwait( false ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] Qux Bar() { using( var foo = m_bar.Get( qux )) { return ( this as IBarProvider ).Bars(); }}", actual.Value.ToFullString() );
	}

	[Test]
	public void SimpleLambda() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { Func<int, Task> bazAsync = async quuxAsync => await fredAsync.Delay( 2*y ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { Func<int, Task> baz = quux => fred.Delay( 2*y ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void SimpleLambdaBlock() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { Func<int, Task> bazAsync = async quuxAsync => { await fredAsync.Delay( 2*y ); await zoopAsync(); } }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { Func<int, Task> baz = quux => { fred.Delay( 2*y ); zoop(); } }", actual.Value.ToFullString() );
	}

	[Test]
	public void WrapInTaskRun() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { string baz = await response.Content.ReadAsStringAsync(); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "#pragma warning disable D2L0018\r\n[Blocking] void Bar() { string baz = Task.Run(() => response.Content.ReadAsStringAsync()).Result; }\n#pragma warning restore D2L0018\r\n", actual.Value.ToFullString() );
	}

	[Test]
	public void TryCatch() {
		var actual = Transform( @"[GenerateSync]
			async Task BarAsync() {
				try {
					baz = await BazAsync();
				} catch( BarException e ) {
					throw new BarException( e );
				} catch ( QuxException e ) {
					throw new QuxException( e );
				}
			}" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( @"[Blocking]
void Bar() {
				try {
					baz = Baz();
				} catch( BarException e ) {
					throw new BarException( e );
				} catch ( QuxException e ) {
					throw new QuxException( e );
				}
			}", actual.Value.ToFullString() );
	}

	[Test]
	public void DeclarationExpression() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { await TryBazAsync( input, out Task<int> result ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Bar() { TryBaz( input,out int result ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void TaskToVoidReturnTypeInvocation() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { return BarAsync(2); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Foo() { { Bar(2); return; } }", actual.Value.ToFullString() );
	}

	[Test]
	public void TaskToVoidReturnTypeNull() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { return null; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Foo() { return; }", actual.Value.ToFullString() );
	}

	[Test]
	public void TaskToVoidReturnTypeDoubleInvocation() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { if( Quux() ) { return BarAsync(2); } return BarAsync(4); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Foo() { if( Quux() ) { { Bar(2); return; } } { Bar(4); return; } }", actual.Value.ToFullString() );
	}

	[Test]
	public void TaskToVoidReturnTypeIncrement() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { return bar++; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Foo() { { bar++; return; } }", actual.Value.ToFullString() );
	}

	[Test]
	public void TaskToVoidReturnTypePreDecrement() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { return --bar; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Foo() { { --bar; return; } }", actual.Value.ToFullString() );
	}

	[Test]
	public void TaskToVoidReturnTypeMemberAccess() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { return Baz.bar; }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] void Foo() { return; }", actual.Value.ToFullString() );
	}

	[Test]
	public void CancellationTokenArgument() {
		var actual = Transform( @"[GenerateSync] Task<Foo> BarAsync() { return FooAsync( CancellationToken.None ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( "[Blocking] Foo Bar() { return Foo( ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void CancellationTokenVariableArgument() {
		var actual = Transform( @"[GenerateSync] Task<Foo> BarAsync() { var ct = new CancellationToken(); return FooAsync( 1, ct, ""test"" ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( @"[Blocking] Foo Bar() { var ct = new CancellationToken(); return Foo( 1,""test"" ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void ParenthesizedAnonymousCreationLambda() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { await BazAsync(() => new { BazAsync = 5, Quux = ""test"" } ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		// For now we are not transforming the variable names
		Assert.AreEqual( @"[Blocking] void Bar() { Baz(() => new { BazAsync = 5,Quux = ""test"" } ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void AnonymousCreation() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { var options = new { size = new { width = GetCurrentWidthAsync(), height = 200 } }; await JsonSerializer.SerializeAsync(options); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( @"[Blocking] void Bar() { var options = new { size = new { width = GetCurrentWidth(),height = 200 } }; JsonSerializer.Serialize(options); }", actual.Value.ToFullString() );
	}

	[Test]
	public void ParenthesizedLambdaInvocation() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { await context.AddDeleteAction( () => Baz.DeleteAsync() ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( @"[Blocking] void Bar() { context.AddDeleteAction( () => Baz.Delete() ); }", actual.Value.ToFullString() );
	}

	[Test]
	public void AsyncParenthesizedBlockLambda() {
		var actual = Transform( @"[GenerateSync] async Task BarAsync() { await CreateAsync( async () => { using( new Context() ) { await BazAsync(); } } ); }" );

		Assert.IsTrue( actual.Success );
		Assert.IsEmpty( actual.Diagnostics );
		Assert.AreEqual( @"[Blocking] void Bar() { Create( () => { using( new Context() ) { Baz(); } } ); }", actual.Value.ToFullString() );
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


	// Modify test in future if we find a way to generate this
	[Test]
	public void TaskToVoidReturnTypeAssignmentFails() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { return m_baz = QuuxAsync(); }" );

		Assert.IsFalse( actual.Success );

		AssertDiagnostics(
			actual.Diagnostics,
			// expected:
			Diagnostics.GenericGeneratorError
		);
	}

	// Modify test in future if we find a way to generate this
	[Test]
	public void TaskToVoidReturnTypeNewObject() {
		var actual = Transform( @"[GenerateSync] Task FooAsync() { return new Task(QuuxAsync); }" );

		Assert.IsFalse( actual.Success );

		AssertDiagnostics(
			actual.Diagnostics,
			// expected:
			Diagnostics.GenericGeneratorError
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
using System.Threading;

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
