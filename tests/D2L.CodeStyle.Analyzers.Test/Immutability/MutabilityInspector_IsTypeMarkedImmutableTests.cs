using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class MutabilityInspector_IsTypeMarkedImmutableTests {

		private const string s_preamble = @"
using D2L.CodeStyle.Annotations;
namespace D2L.CodeStyle.Annotations {
	public class Objects {
		public class Immutable : Attribute {}
	}
}
";
		private TestSymbol<ITypeSymbol> CompileAndGetFooType( string source ) {
			source = $"namespace D2L {{ {source} }}";
			source = s_preamble + source;

			var compilation = Compile( source );
			var symbol = compilation.GetSymbolsWithName(
				predicate: n => n == "Foo",
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();

			Assert.IsNotNull( symbol );
			Assert.AreNotEqual( TypeKind.Error, symbol.TypeKind );

			return new TestSymbol<ITypeSymbol>( symbol, compilation );
		}

		[Test]
		public void IsTypeMarkedImmutable_No_ReturnsFalse() {
			var type = CompileAndGetFooType( "class Foo {}" );

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			Assert.IsFalse( type.Symbol.IsTypeMarkedImmutable() );
		}

		[Test]
		public void IsTypeMarkedImmutable_Yes_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				[Objects.Immutable] class Foo {}"
			);

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			Assert.IsTrue( type.Symbol.IsTypeMarkedImmutable() );
		}

		[Test]
		public void IsTypeMarkedImmutable_InterfaceIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : IFoo {} 
				[Objects.Immutable] interface IFoo {}"
			);

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.Symbol.MetadataName );
			Assert.IsTrue( type.Symbol.IsTypeMarkedImmutable() );
		}

		[Test]
		public void IsTypeMarkedImmutable_OneOfTheInterfaceIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : IFoo1, IFoo2 {} 
				interface IFoo1 {}
				[Objects.Immutable] interface IFoo2 { }"
			);

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.Symbol.MetadataName );
			Assert.IsTrue( type.Symbol.IsTypeMarkedImmutable() );
		}

		[Test]
		public void IsTypeMarkedImmutable_SomeTopLevelInterfaceIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : IFoo {} 
				interface IFoo : IFooTop {} 
				[Objects.Immutable] interface IFooTop {}"
			);

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.Symbol.MetadataName );
			Assert.IsTrue( type.Symbol.IsTypeMarkedImmutable() );
		}


		[Test]
		public void IsTypeMarkedImmutable_ParentClassIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : FooBase {} 
				[Objects.Immutable] class FooBase {}"
			);

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.Symbol.MetadataName );
			Assert.IsTrue( type.Symbol.IsTypeMarkedImmutable() );
		}

		[Test]
		public void IsTypeMarkedImmutable_SomeTopLevelParentClassIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : FooBase {} 
				class FooBase : FooBaseOfBase {}
				[Objects.Immutable] class FooBaseOfBase { }"
			);

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.Symbol.MetadataName );
			Assert.IsTrue( type.Symbol.IsTypeMarkedImmutable() );
		}
	}
}
