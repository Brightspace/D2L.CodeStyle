using NUnit.Framework;
using System.Collections.Generic;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[TestFixture]
	class MutabilityInspectionResultFormatterTests {

		private readonly MutabilityInspectionResultFormatter m_formatter = new MutabilityInspectionResultFormatter();

		private static readonly IEnumerable<TestCaseData> m_testCases = new[] {
			new TestCaseData(
				MutabilityInspectionResult.NotMutable(),
				string.Empty
			).SetName("not mutable"),

            // field cases

            new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.Member, MutabilityCause.IsNotReadonly),
				"'foo' is not read-only"
			).SetName("field is not read-only"),

            // type cases

            new TestCaseData(
				MutabilityInspectionResult.Mutable(null, "bar", MutabilityTarget.Type, MutabilityCause.IsAnArray),
				"its type ('bar') is an array"
			).SetName("type is an array, null member"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("", "bar", MutabilityTarget.Type, MutabilityCause.IsAnArray),
				"its type ('bar') is an array"
			).SetName("type is an array, empty member"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.Type, MutabilityCause.IsAnArray),
				"'foo''s type ('bar') is an array"
			).SetName("type is an array"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.Type, MutabilityCause.IsDynamic),
				"'foo''s type ('bar') is dynamic"
			).SetName("type is dynamic"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.Type, MutabilityCause.IsAnInterface),
				"'foo''s type ('bar') is an interface that is not marked with `[Objects.Immutable]`"
			).SetName("type is an interface"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.Type, MutabilityCause.IsNotSealed),
				"'foo''s type ('bar') is not sealed"
			).SetName("type is not sealed"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.Type, MutabilityCause.IsPotentiallyMutable),
				"'foo''s type ('bar') is not deterministically immutable"
			).SetName("type is not not deterministically immutable"),

            // type argument cases

			new TestCaseData(
				MutabilityInspectionResult.Mutable(null, "bar", MutabilityTarget.TypeArgument, MutabilityCause.IsAnArray),
				"its type argument ('bar') is an array"
			).SetName("type argument is an array, null member"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("", "bar", MutabilityTarget.TypeArgument, MutabilityCause.IsAnArray),
				"its type argument ('bar') is an array"
			).SetName("type argument is an array, empty member"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.TypeArgument, MutabilityCause.IsAnArray),
				"'foo''s type argument ('bar') is an array"
			).SetName("type argument is an array"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.TypeArgument, MutabilityCause.IsDynamic),
				"'foo''s type argument ('bar') is dynamic"
			).SetName("type argument is dynamic"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.TypeArgument, MutabilityCause.IsAnInterface),
				"'foo''s type argument ('bar') is an interface that is not marked with `[Objects.Immutable]`"
			).SetName("type argument is an interface"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.TypeArgument, MutabilityCause.IsNotSealed),
				"'foo''s type argument ('bar') is not sealed"
			).SetName("type argument is not sealed"),

			new TestCaseData(
				MutabilityInspectionResult.Mutable("foo", "bar", MutabilityTarget.TypeArgument, MutabilityCause.IsPotentiallyMutable),
				"'foo''s type argument ('bar') is not deterministically immutable"
			).SetName("type argument is not not deterministically immutable"),

		};

		[Test, TestCaseSource( nameof( m_testCases ) )]
		public void Format_NotMutable_FormatsCorrectly(
			MutabilityInspectionResult result,
			string expected
		) {
			var formatted = m_formatter.Format( result );

			Assert.AreEqual( expected, formatted );
		}
	}
}
