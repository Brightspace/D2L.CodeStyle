using D2L.CodeStyle.Analyzers.UnsafeStatics;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class AnalyzedStatic {
		public const string CAUSE_MUTABLE_DECLARATION = "DeclarationIsMutable";
		public const string CAUSE_MUTABLE_TYPE = "TypeIsMutable";

		public readonly string ProjectName;
		public readonly string FilePath;
		public readonly int LineNumber;
		public readonly string FieldOrPropName;
		public readonly string FieldOrPropType;
		public readonly string Cause;

		public AnalyzedStatic( string projectName, Diagnostic diag ) {
			ProjectName = projectName;
			FilePath = diag.Location.SourceTree.FilePath;
			LineNumber = diag.Location.GetMappedLineSpan().Span.Start.Line;
			FieldOrPropName = diag.Properties[UnsafeStaticsAnalyzer.PROPERTY_FIELDORPROPNAME];
			FieldOrPropType = diag.Properties[UnsafeStaticsAnalyzer.PROPERTY_OFFENDINGTYPE];

			if( FieldOrPropType == "it" ) {
				FieldOrPropType = null; // analyzer doesn't expose type if declaration is unsafe
				Cause = CAUSE_MUTABLE_DECLARATION;
			} else {
				Cause = CAUSE_MUTABLE_TYPE;
			}
		}
	}
}
