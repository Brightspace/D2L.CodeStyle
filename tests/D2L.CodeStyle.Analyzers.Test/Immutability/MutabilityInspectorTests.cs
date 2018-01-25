using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[TestFixture]
	public class MutabilityInspectorTests {
		[Test]
		public void InspectType_PrimitiveType_NotMutable() {
			var field = Field( "uint foo" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( field.Symbol.Type );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_NullablePrimitiveType_NotMutable() {
			var field = Field( "uint? foo" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( field.Symbol.Type );

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

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( realType );

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

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var actual = inspector.InspectType( field.Symbol.Type );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_KnownImmutableType_False() {
			var field = Field( "string random" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( field.Symbol.Type );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_Interface_True() {
			var type = Type( "interface foo {}" );

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.Mutable(
				null,
				$"{RootNamespace}.foo",
				MutabilityTarget.Type,
				MutabilityCause.IsAnInterface
			);

			var actual = inspector.InspectType( type.Symbol );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_Enum_False() {
			var type = Type( "enum blah {}" );

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( type.Symbol );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_NonSealedClass_True() {
			var type = Type( "class foo {}" );

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.Mutable(
				null,
				$"{RootNamespace}.foo",
				MutabilityTarget.Type,
				MutabilityCause.IsNotSealed
			);

			var actual = inspector.InspectType( type.Symbol );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_SealedClass_False() {
			var type = Type( "sealed class foo {}" );

			var inspector = new MutabilityInspector(
				type.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( type.Symbol );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtMembersInDeclaredType() {
			var field = Field( "public string random" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.Mutable(
				"random",
				"System.String",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = inspector.InspectType( field.Symbol.ContainingType );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_DoesNotLookAtMembersInExternalType() {
			var field = Field( "public readonly System.Text.StringBuilder random" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.Mutable(
				null,
				"System.Text.StringBuilder",
				MutabilityTarget.Type,
				MutabilityCause.IsAnExternalUnmarkedType
			);

			var actual = inspector.InspectType( field.Symbol.Type );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtFieldsInNonExternalType() {
			var field = Field( "public string random" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.Mutable(
				"random",
				"System.String",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = inspector.InspectType( field.Symbol.ContainingType );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_LooksAtPropertiesInNonExternalType() {
			var prop = Property( "public string random { get; set; }" );

			var inspector = new MutabilityInspector(
				prop.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.Mutable(
				"random",
				"System.String",
				MutabilityTarget.Member,
				MutabilityCause.IsNotReadonly
			);

			var actual = inspector.InspectType( prop.Symbol.ContainingType );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_ImmutableGenericCollectionWithValueTypeElement_ReturnsFalse() {
			var field = Field( "private readonly System.Collections.Immutable.ImmutableArray<int> random" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( field.Symbol.Type );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_IEnumerableGenericCollectionWithImmutableElement_ReturnsFalse() {
			var field = Field( "private readonly System.Collections.Generic.IEnumerable<int> random" );

			var inspector = new MutabilityInspector(
				field.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable();

			var actual = inspector.InspectType( field.Symbol.Type );

			AssertResultsAreEqual( expected, actual );
		}

		[Test]
		public void InspectType_TypeWithFuncProperty_ReturnsMutable() {
			var prop = Property( "public Func<string> StringGetter { get; }" );

			var inspector = new MutabilityInspector(
				prop.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.Mutable(
				"StringGetter",
				"System.Func",
				MutabilityTarget.Type,
				MutabilityCause.IsADelegate
			);

			var actual = inspector.InspectType( prop.Symbol.ContainingType );

			AssertResultsAreEqual( expected, actual );
		}

		private const string AnnotationsPreamble = @"
using System;
using D2L.CodeStyle.Annotations;
using static D2L.CodeStyle.Annotations.Objects;
 
namespace D2L.CodeStyle.Annotations {
	public enum Because {
		ItHasntBeenLookedAt,
		ItsSketchy,
		ItsStickyDataOhNooo,
		WeNeedToMakeTheAnalyzerConsiderThisSafe,
		ItsUgly,
		ItsOnDeathRow
	}

	public enum UndiffBucket {
		Core
	}
 
	public static class Mutability {
		public sealed class UnauditedAttribute : Attribute {
 
			public UnauditedAttribute( Because why ) { }

			public UnauditedAttribute( Because why, UndiffBucket bucket ) { }
		}
	}

	public static class Objects {
		public sealed class Immutable : Attribute {
		}
	}
}
";

		[Test]
		public void InspectType_TypeHasNoUnauditedMembers_NoUnauditedReasons() {

			const string source = AnnotationsPreamble + @"
sealed class Foo {
	public int Bar { get; }
}

[Immutable]
sealed class Bar { }
";

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( source );

			AssertUnauditedReasonsResult( ty );
		}

		[Test]
		public void InspectType_TypeHasOneUnauditedMember_ReturnsUnauditedReason() {

			const string source = AnnotationsPreamble + @"
sealed class Foo {
	[Mutability.Unaudited( Because.ItsSketchy )]
	public Func<string> Bar { get; }
}
";

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( source );

			AssertUnauditedReasonsResult( ty, "ItsSketchy" );
		}

		[Test]
		public void InspectType_TypeHasMultipleDifferentUnauditedMembers_ReturnsAllUnauditedReasons() {

			const string source = AnnotationsPreamble + @"
sealed class Foo {
	[Mutability.Unaudited( Because.ItsSketchy )]
	public Func<string> Bar { get; }
	[Mutability.Unaudited( Because.ItsUgly )]
	public Func<string> Baz { get; }
}
";

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( source );

			AssertUnauditedReasonsResult( ty, "ItsSketchy", "ItsUgly" );
		}

		[Test]
		public void InspectType_TypeHasMultipleIdenticalUnauditedMembers_ReturnsAllUnauditedReasons() {

			const string source = AnnotationsPreamble + @"
sealed class Foo {
	[Mutability.Unaudited( Because.ItsSketchy )]
	public Func<string> Bar { get; }
	[Mutability.Unaudited( Because.ItsSketchy )]
	public Func<string> Baz { get; }
}
";

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( source );

			AssertUnauditedReasonsResult( ty, "ItsSketchy" );
		}

		private TestSymbol<ITypeSymbol> CompileAndGetFooType( string source ) {
			var compilation = Compile( source );

			var ty = compilation.GetSymbolsWithName(
				predicate: n => true,
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault( s => s.Name == "Foo" );

			return new TestSymbol<ITypeSymbol>( ty, compilation );
		}

		private void AssertUnauditedReasonsResult( TestSymbol<ITypeSymbol> ty, params string[] expectedUnauditedReasons ) {

			var inspector = new MutabilityInspector(
				ty.Compilation,
				KnownImmutableTypes.Default
			);

			var expected = MutabilityInspectionResult.NotMutable(
				ImmutableHashSet.Create( expectedUnauditedReasons )
			);

			var actual = inspector.InspectType( ty.Symbol );

			AssertResultsAreEqual( expected, actual );
		}

		private void AssertResultsAreEqual( MutabilityInspectionResult expected, MutabilityInspectionResult actual ) {
			Assert.AreEqual( expected.IsMutable, actual.IsMutable, "IsMutable does not match" );
			Assert.AreEqual( expected.MemberPath, actual.MemberPath, "MemberPath does not match" );
			Assert.AreEqual( expected.Target, actual.Target, "Target does not match" );
			Assert.AreEqual( expected.Cause, actual.Cause, "Cause does not match" );
			Assert.AreEqual( expected.TypeName, actual.TypeName, "TypeName does not match" );
			CollectionAssert.AreEquivalent( expected.SeenUnauditedReasons, actual.SeenUnauditedReasons, "SeenUnauditedReasons did not match" );
		}

	}
}
