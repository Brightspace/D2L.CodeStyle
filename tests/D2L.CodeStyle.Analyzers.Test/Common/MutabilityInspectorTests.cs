using System.Linq;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Common.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Common {

	[TestFixture]
	public class MutabilityInspectorTests {

		private readonly MutabilityInspector m_inspector = new MutabilityInspector( KnownImmutableTypes.Default );

		[Test]
		public void InspectType_PrimitiveType_NotMutable() {
			var field = Field( "uint foo" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( field.Symbol.Type, field.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_NullablePrimitiveType_NotMutable() {
			var field = Field( "uint? foo" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( field.Symbol.Type, field.Compilation );

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
			var field = type.Symbol.GetMembers().FirstOrDefault( m => m is IFieldSymbol );
			Assert.IsNotNull( field );
			var realType = ( field as IFieldSymbol ).Type;
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( realType, type.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_ArrayType_True() {
			var field = Field( "int[] random" );
			var expected = MutabilityInspectionResult.Mutable(
				null,
				"System.Int32[]",
				MutabilityTarget.Type,
				MutabilityCause.IsAnArray
			);

			var actual = m_inspector.InspectType( field.Symbol.Type, field.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_KnownImmutableType_False() {
			var field = Field( "string random" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( field.Symbol.Type, field.Compilation );

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

			var actual = m_inspector.InspectType( type.Symbol, type.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_Enum_False() {
			var type = Type( "enum blah {}" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type.Symbol, type.Compilation );

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

			var actual = m_inspector.InspectType( type.Symbol, type.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_SealedClass_False() {
			var type = Type( "sealed class foo {}" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( type.Symbol, type.Compilation );

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

			var actual = m_inspector.InspectType( field.Symbol.ContainingType, field.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_DoesNotLookAtMembersInExternalType() {
			var field = Field( "public readonly System.Text.StringBuilder random" );

			var expected = MutabilityInspectionResult.Mutable(
				null,
				"System.Text.StringBuilder",
				MutabilityTarget.Type,
				MutabilityCause.IsAnExternalUnmarkedType
			);

			var actual = m_inspector.InspectType( field.Symbol.Type, field.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtFieldsInNonExternalType() {
			var field = Field( "public string random" );

			var expected = MutabilityInspectionResult.Mutable(
				"random",
				"System.String",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = m_inspector.InspectType( field.Symbol.ContainingType, field.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtPropertiesInNonExternalType() {
			var prop = Property( "public string random { get; set; }" );

			var expected = MutabilityInspectionResult.Mutable(
				"random",
				"System.String",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = m_inspector.InspectType( prop.Symbol.ContainingType, prop.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_ImmutableGenericCollectionWithValueTypeElement_ReturnsFalse() {
			var field = Field( "private readonly System.Collections.Immutable.ImmutableArray<int> random" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( field.Symbol.Type, field.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_IEnumerableGenericCollectionWithImmutableElement_ReturnsFalse() {
			var field = Field( "private readonly System.Collections.Generic.IEnumerable<int> random" );
			var expected = MutabilityInspectionResult.NotMutable();

			var actual = m_inspector.InspectType( field.Symbol.Type, field.Compilation );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_TypeWithFuncProperty_ReturnsMutable() {
			var prop = Property( "public Func<string> StringGetter { get; }" );
			var expected = MutabilityInspectionResult.Mutable(
				"StringGetter",
				"System.Func",
				MutabilityTarget.Type,
				MutabilityCause.IsADelegate
			);

			var actual = m_inspector.InspectType( prop.Symbol.ContainingType, prop.Compilation );

			AssertResultsAreEqual( expected, actual );
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
