using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal sealed class AnalyzedStatic {
		public readonly string ProjectName;
		public readonly string FilePath;
		public readonly int LineNumber;
		public readonly string FieldOrPropName;
		public readonly string FieldOrPropType;
		public readonly string Cause;

		public AnalyzedStatic( 
			string projectName, 
			string filePath, 
			int lineNumber, 
			string name, 
			string typeName,
			string cause
		) {
			ProjectName = projectName;
			FilePath = filePath;
			LineNumber = lineNumber;
			FieldOrPropName = name;
			FieldOrPropType = typeName;
		}

		public AnalyzedStatic( IFieldSymbol symbol, string cause ) : this(
			projectName: symbol.ContainingAssembly.Name,
			filePath: symbol.Locations[ 0 ].SourceTree.FilePath,
			lineNumber: symbol.Locations[ 0 ].GetMappedLineSpan().Span.Start.Line,
			name: symbol.Name,
			typeName: symbol.Type.GetFullTypeName(),
			cause: cause
		) { }

		public AnalyzedStatic( IPropertySymbol symbol, string cause ) : this(
			projectName: symbol.ContainingAssembly.Name,
			filePath: symbol.Locations[ 0 ].SourceTree.FilePath,
			lineNumber: symbol.Locations[ 0 ].GetMappedLineSpan().Span.Start.Line,
			name: symbol.Name,
			typeName: symbol.Type.GetFullTypeName(),
			cause: cause
		) { }
	}
}
