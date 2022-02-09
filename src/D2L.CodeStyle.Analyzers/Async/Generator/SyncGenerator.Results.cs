using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Async.Generator;

internal sealed partial class SyncGenerator {
	// All of the intermediate results generated in the incremental pipeline.
	// All of these should implement value-based equality. So use records and
	// make sure the things they hold implement equality.

	/// <summary>
	/// The generated syntax for a single method.
	/// </summary>
	private readonly record struct MethodGenerationResult(
		MethodDeclarationSyntax Original,
		string GeneratedSyntax,
		ImmutableArray<Diagnostic> Diagnostics
	);

	/// <summary>
	/// The individual methods are concatenated into one output file.
	/// </summary>
	private readonly record struct FileGenerationResult(
		string HintName,
		string GeneratedSource,
		ImmutableArray<Diagnostic> Diagnostics
	);
}
