﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests {

	public sealed record SpecTest(
		string Name,
		string Source,
		ImmutableArray<AdditionalText> AdditionalFiles,
		ImmutableArray<MetadataReference> MetadataReferences,
		ImmutableDictionary<string, DiagnosticDescriptor> DiagnosticDescriptors
	) {

		public override string ToString() {
			return Name;
		}
	}
}
