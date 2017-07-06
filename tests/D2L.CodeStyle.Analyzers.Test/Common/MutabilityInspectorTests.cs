using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Common.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Common {

	[TestFixture]
	public class MutabilityInspectorTests {

		private readonly MutabilityInspector m_inspector = new MutabilityInspector( KnownImmutableTypes.Default );

		[Test]
		public void InspectType_PrimitiveType_NotMutable() {
			var type = Field( "uint foo" ).Type;
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_NullablePrimitiveType_NotMutable() {
			var type = Field( "uint? foo" ).Type;
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_NullableNonPrimitiveType_NotMutable() {
			var type = Type( @"
				class Test {
					struct Hello { }
					Hello? nullable;
				}"
			);
			var field = type.GetMembers().FirstOrDefault( m => m is IFieldSymbol );
			Assert.IsNotNull( field );
			type = ( field as IFieldSymbol ).Type;
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_ArrayType_True() {
			var type = Field( "int[] random" ).Type;
			var expected = MutabilityInspectionResult.Mutable(
				null,
				"System.Int32[]",
				MutabilityTarget.Type,
				MutabilityCause.IsAnArray
			);

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_KnownImmutableType_False() {
			var type = Field( "string random" ).Type;
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_Interface_True() {
			var type = Type( "interface foo {}" );
			var expected = MutabilityInspectionResult.Mutable(
				null,
				$"{RootNamespace}.foo",
				MutabilityTarget.Type,
				MutabilityCause.IsAnInterface
			);

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_Enum_False() {
			var type = Type( "enum blah {}" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_NonSealedClass_True() {
			var type = Type( "class foo {}" );
			var expected = MutabilityInspectionResult.Mutable(
				null,
				$"{RootNamespace}.foo",
				MutabilityTarget.Type,
				MutabilityCause.IsNotSealed
			);

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_SealedClass_False() {
			var type = Type( "sealed class foo {}" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtMembersInDeclaredType() {
			var field = Field( "public string random" );

			var expected = MutabilityInspectionResult.Mutable(
				"random",
				"System.String",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = m_inspector.InspectType( field.ContainingType, field.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtMembersInExternalType() {
			var field = Field( "public readonly System.Text.StringBuilder random" );

			var expected = MutabilityInspectionResult.Mutable(
				"Capacity",
				"System.Int32",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = m_inspector.InspectType( field.Type, field.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksFieldsInType() {
			var field = Field( "public readonly System.Text.StringBuilder random" );

			var expected = MutabilityInspectionResult.Mutable(
				"random.Capacity",
				"System.Int32",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = m_inspector.InspectType( field.ContainingType, field.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtPropertiesInType() {
			var prop = Property( "public string random { get; set; }" );

			var expected = MutabilityInspectionResult.Mutable(
				$"random",
				"System.String",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = m_inspector.InspectType( prop.ContainingType, prop.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_ImmutableGenericCollectionWithValueTypeElement_ReturnsFalse() {
			var type = Field( "private readonly System.Collections.Immutable.ImmutableArray<int> random" ).Type;
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_IEnumerableGenericCollectionWithImmutableElement_ReturnsFalse() {
			var type = Field( "private readonly System.Collections.Generic.IEnumerable<int> random" ).Type;
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_TypeWithFuncProperty_ReturnsMutable() {
			var type = Property( "public Func<string> StringGetter { get; }" ).ContainingType;
			var expected = MutabilityInspectionResult.Mutable(
				"StringGetter",
				"System.Func",
				MutabilityTarget.Type,
				MutabilityCause.IsADelegate
			);

			var actual = m_inspector.InspectType( type, type.ContainingAssembly );

			AssertResultsAreEqual( expected, actual );
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

		private void AssertResultsAreEqual( MutabilityInspectionResult expected, MutabilityInspectionResult actual ) {
			Assert.AreEqual( expected.IsMutable, actual.IsMutable, "IsMutable does not match" );
			Assert.AreEqual( expected.MemberPath, actual.MemberPath, "MemberPath does not match" );
			Assert.AreEqual( expected.Target, actual.Target, "Target does not match" );
			Assert.AreEqual( expected.Cause, actual.Cause, "Cause does not match" );
			Assert.AreEqual( expected.TypeName, actual.TypeName, "TypeName does not match" );
		}

	}
}
