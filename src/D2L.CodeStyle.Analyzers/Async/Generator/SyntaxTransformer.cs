#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace D2L.CodeStyle.Analyzers.Async.Generator;

/// <summary>
/// Base class for things that transform a SyntaxNode into another and collect
/// diagnostics. This is useful for writing generators that copy+paste code
/// with only light modifications.
/// </summary>
public abstract class SyntaxTransformer {
	protected readonly SemanticModel m_model;
	private readonly ImmutableArray<Diagnostic>.Builder m_diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

	/// <summary>
	/// A name for the generator that will be shown to the user if we crash
	/// </summary>
	public SyntaxTransformer( SemanticModel model ) {
		m_model = model;
	}

	/// <summary>
	/// Suggested return type for public transformation methods.
	/// </summary>
	public record struct TransformResult<T>(
		T? Value,
		ImmutableArray<Diagnostic> Diagnostics
	) {
		public bool Success => Diagnostics.IsEmpty;

		public TransformResult( ImmutableArray<Diagnostic> diagnostics ) : this(
			Value: default,
			Diagnostics: diagnostics
		) {}

		public TransformResult( T value ) : this(
			Value: value,
			Diagnostics: ImmutableArray<Diagnostic>.Empty
		) { }
	}

	protected TransformResult<T> GetResult<T>( T value ) {
		var result = m_diagnostics.ToImmutable();
		m_diagnostics.Clear();

		if( result.IsEmpty ) {
			return new( value );
		}

		return new( result );
	}

	/// <summary>
	/// Report a diagnostic and continue.
	/// </summary>
	protected void ReportDiagnostic(
		DiagnosticDescriptor descriptor,
		Location location,
		params object[] messageArgs
	) {
		var diag = Diagnostic.Create( descriptor, location, messageArgs );

		m_diagnostics.Add( diag );
	}

	/// <summary>
	/// Reports an error in our generator to the user.
	/// </summary>
	/// <param name="location"></param>
	/// <param name="what"></param>
	protected void GeneratorError( Location location, string what ) =>
		ReportDiagnostic( Diagnostics.GenericGeneratorError, location, GetType().Name, what );

	/// <summary>
	/// Transform every element of a SyntaxList and filter out nulls.
	/// </summary>
	protected SyntaxList<U> TransformAll<T, U>(
		SyntaxList<T> input,
		Func<T, U?> transformer
	) where T : SyntaxNode
	  where U : SyntaxNode
	=> SyntaxFactory.List(
		(IEnumerable<U>)input // Cast away the ? because of the Where filter
			.Select( transformer )
			.Where( NotNull )
	);

	/// <summary>
	/// Transform every element of a SeparatedSyntaxList and filter out nulls.
	/// </summary>
	protected SeparatedSyntaxList<U> TransformAll<T, U>(
		SeparatedSyntaxList<T> input,
		Func<T, U?> transformer
	) where T : SyntaxNode
	  where U : SyntaxNode
	=> SyntaxFactory.SeparatedList(
		(IEnumerable<U>)input // Cast away the ? because of the Where filter
			.Select( transformer )
			.Where( NotNull )
	);

	/// <summary>
	/// Transform every element of a SyntaxTokenList and filter out nulls.
	/// </summary>
	protected SyntaxTokenList TransformAll(
		SyntaxTokenList input,
		Func<SyntaxToken, SyntaxToken?> transformer
	) => SyntaxFactory.TokenList(
		input.Select( transformer )
			.Where( NotNull )
			.Select( static t => t!.Value )
	);

	private static bool NotNull<T>( T? t ) => t != null;
}
