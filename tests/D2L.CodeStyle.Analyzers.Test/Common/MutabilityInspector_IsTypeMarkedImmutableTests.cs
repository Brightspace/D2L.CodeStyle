using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Common.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Common {
	internal sealed class MutabilityInspector_IsTypeMarkedImmutableTests {

		private readonly MutabilityInspector m_inspector = new MutabilityInspector( KnownImmutableTypes.Default );

		private const string s_preamble = @"
using D2L.CodeStyle.Annotations;
namespace D2L.CodeStyle.Annotations {
	public class Objects {
		public class Immutable : Attribute {}
	}
}
";

		private ITypeSymbol CompileAndGetFooType( string source ) {
			source = $"namespace D2L {{ {source} }}";
			source = s_preamble + source;

			var compilation = Compile( source );
			var toReturn = compilation.GetSymbolsWithName(
				predicate: n => n == "Foo",
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();

			Assert.IsNotNull( toReturn );
			Assert.AreNotEqual( TypeKind.Error, toReturn.TypeKind );

			return toReturn;
		}

		[Test]
		public void IsTypeMarkedImmutable_No_ReturnsFalse() {
			var type = CompileAndGetFooType( "class Foo {}" );

			Assert.IsFalse( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_Yes_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				[Objects.Immutable] class Foo {}"
			);

			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_InterfaceIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : IFoo {} 
				[Objects.Immutable] interface IFoo {}"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_OneOfTheInterfaceIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : IFoo1, IFoo2 {} 
				interface IFoo1 {}
				[Objects.Immutable] interface IFoo2 { }"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_SomeTopLevelInterfaceIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : IFoo {} 
				interface IFoo : IFooTop {} 
				[Objects.Immutable] interface IFooTop {}"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}


		[Test]
		public void IsTypeMarkedImmutable_ParentClassIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : FooBase {} 
				[Objects.Immutable] class FooBase {}"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_SomeTopLevelParentClassIs_ReturnsTrue() {
			var type = CompileAndGetFooType( @"
				class Foo : FooBase {} 
				class FooBase : FooBaseOfBase {}
				[Objects.Immutable] class FooBaseOfBase { }"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}
	}
}
