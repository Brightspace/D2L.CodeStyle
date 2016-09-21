using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace D2L.CodeStyle.Analysis {

	[TestFixture]
	public class MutabilityInspectorTests {

		private readonly MutabilityInspector m_inspector = new MutabilityInspector();

		private static CSharpCompilation Compile( string source ) {
			var tree = CSharpSyntaxTree.ParseText( source );
			var compilation = CSharpCompilation.Create(
				assemblyName: "TestAssembly",
				syntaxTrees: new[] { tree },
				references: new[] {
					MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ),
					MetadataReference.CreateFromFile( typeof( ImmutableArray ).Assembly.Location )
				}
			);
			return compilation;
		}

		private ITypeSymbol Type( string text ) {
			var source = $"namespace D2L {{ {text} }}";
			var compilation = Compile( source );

			var toReturn = compilation.GetSymbolsWithName(
				predicate: n => true,
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			Assert.AreNotEqual( TypeKind.Error, toReturn.TypeKind );
			return toReturn;
		}

		private IFieldSymbol Field( string text ) {
			var type = Type( "sealed class Fake { " + text + "; }" );

			var toReturn = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );
			return toReturn;
		}

		private IPropertySymbol Property( string text ) {
			var type = Type( "sealed class Fake { " + text + "; }" );

			var toReturn = type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );
			return toReturn;
		}

		[Test]
		public void IsFieldMutable_Private_True() {
			var field = Field( "private int[] random" );

			Assert.IsTrue( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsFieldMutable_Readonly_False() {
			var field = Field( "readonly int[] random" );

			Assert.IsFalse( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsFieldMutable_PublicReadonly_False() {
			var field = Field( "public readonly int[] random" );

			Assert.IsFalse( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsFieldMutable_Public_True() {
			var field = Field( "public int[] random" );

			Assert.IsTrue( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsPropertyMutable_Private_True() {
			var prop = Property( "private int random { get; set; }" );

			Assert.IsTrue( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsPropertyMutable_Readonly_False() {
			var prop = Property( "int random { get; }" );

			Assert.IsFalse( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsPropertyMutable_PrivateSetter_True() {
			var prop = Property( "int random { get; private set; }" );

			Assert.IsTrue( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsPropertyMutable_PublicWithSetter_True() {
			var prop = Property( "public int random { get; set; }" );

			Assert.IsTrue( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsTypeMutable_ValueType_False() {
			var type = Type( "struct random { string hello; }" );

			Assert.IsFalse( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_ArrayType_True() {
			var type = Field( "int[] random" ).Type;

			Assert.IsTrue( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_KnownImmutableType_False() {
			var type = Field( "string random" ).Type;

			Assert.IsFalse( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_Interface_True() {
			var type = Type( "interface foo {}" );

			Assert.IsTrue( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_NonSealedClass_True() {
			var type = Type( "class foo {}" );

			Assert.IsTrue( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_SealedClass_False() {
			var type = Type( "sealed class foo {}" );

			Assert.IsFalse( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_LooksAtFieldsInType() {
			var field = Field( "public string random" );
			Assert.IsTrue( m_inspector.IsFieldMutable( field ) );

			Assert.IsTrue( m_inspector.IsTypeMutable( field.ContainingType ) );
		}

		[Test]
		public void IsTypeMutable_LooksAtMutabilityOfTypeOfField() {
			var field = Field( "public readonly System.Text.StringBuilder random" );
			Assert.IsFalse( m_inspector.IsFieldMutable( field ) );

			Assert.IsTrue( m_inspector.IsTypeMutable( field.ContainingType ) );
		}

		[Test]
		public void IsTypeMutable_LooksAtPropertiesInType() {
			var prop = Property( "public string random { get; set; }" );
			Assert.IsTrue( m_inspector.IsPropertyMutable( prop ) );

			Assert.IsTrue( m_inspector.IsTypeMutable( prop.ContainingType ) );
		}

		[Test]
		public void IsTypeMutable_LooksAtMutabilityOfTypeOfProperty() {
			var property = Property( "public System.Text.StringBuilder random { get; }" );
			Assert.IsFalse( m_inspector.IsPropertyMutable( property ) );

			Assert.IsTrue( m_inspector.IsTypeMutable( property.Type ) );
		}

		[Test]
		public void IsTypeMutable_NonExistentType_ThrowsException() {
			var type = Property( "public System.Text.StringBuilder random { get; }" ).Type;

			Assert.IsTrue( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_ImmutableGenericCollectionWithValueTypeElement_ReturnsFalse() {
			var type = Field( "private readonly System.Collections.Immutable.ImmutableArray<int> random" ).Type;

			Assert.IsFalse( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_No_ReturnsFalse() {
			var type = Type( "class Foo {}" );

			Assert.IsFalse( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_Yes_ReturnsTrue() {
			var type = Type( "[Immutable] class Foo {}" );

			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_InterfaceIs_ReturnsTrue() {
			var type = Type( @"
				class Foo : IFoo {} 
				[Immutable] interface IFoo {} "
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_OneOfTheInterfaceIs_ReturnsTrue() {
			var type = Type( @"
				class Foo : IFoo1, IFoo2 {} 
				interface IFoo1 {}
				[Immutable] interface IFoo2 { } "
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_SomeTopLevelInterfaceIs_ReturnsTrue() {
			var type = Type( @"
				class Foo : IFoo {} 
				interface IFoo : IFooTop {} 
				[Immutable] interface IFooTop {}"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}


		[Test]
		public void IsTypeMarkedImmutable_ParentClassIs_ReturnsTrue() {
			var type = Type( @"
				class Foo : FooBase {} 
				[Immutable] class FooBase {}"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

		[Test]
		public void IsTypeMarkedImmutable_SomeTopLevelParentClassIs_ReturnsTrue() {
			var type = Type( @"
				class Foo : FooBase {} 
				class FooBase : FooBaseOfBase {}
				[Immutable] class FooBaseOfBase { }"
			);

			// we have multiple types defined, so ensure that we're asserting on the correct one first.
			Assert.AreEqual( "Foo", type.MetadataName );
			Assert.IsTrue( m_inspector.IsTypeMarkedImmutable( type ) );
		}

	}
}
