﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Async.Generator;

/// <summary>
/// Base class for things that transform a SyntaxNode into another and collect
/// diagnostics. This is useful for writing generators that copy+paste code
/// with only light modifications.
/// </summary>
public abstract class SyntaxTransformer {

	protected SemanticModel Model { get; }
	protected CancellationToken Token { get; }

	private readonly ImmutableArray<Diagnostic>.Builder m_diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

	/// <summary>
	/// A name for the generator that will be shown to the user if we crash
	/// </summary>
	public SyntaxTransformer( SemanticModel model, CancellationToken token ) {
		Model = model;
		Token = token;
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
	protected static SyntaxList<TItemOut> TransformAll<TItemIn, TItemOut>(
		SyntaxList<TItemIn> input,
		Func<TItemIn, TItemOut?> transformer
	) where TItemIn : SyntaxNode
	  where TItemOut : SyntaxNode
		=> SyntaxFactory.List( TransformAllCore( input, transformer ) );

	/// <summary>
	/// Transform every element of a SeparatedSyntaxList and filter out nulls.
	/// </summary>
	protected static SeparatedSyntaxList<TItemOut> TransformAll<TItemIn, TItemOut>(
		SeparatedSyntaxList<TItemIn> input,
		Func<TItemIn, TItemOut?> transformer
	) where TItemIn : SyntaxNode
	  where TItemOut : SyntaxNode
		=> SyntaxFactory.SeparatedList( TransformAllCore( input, transformer ) );

	protected static TArgList TransformAll<TArgList>(
		TArgList input,
		Func<ArgumentSyntax, ArgumentSyntax?> transformer
	) where TArgList : BaseArgumentListSyntax
		=> (TArgList)input.WithArguments( TransformAll( input.Arguments, transformer ) );

	/// <summary>
	/// Transform every element of a SyntaxTokenList and filter out default tokens.
	/// </summary>
	protected static SyntaxTokenList TransformAll(
		SyntaxTokenList input,
		Func<SyntaxToken, SyntaxToken?> transformer
	) => SyntaxFactory.TokenList( TransformAllCore( input, transformer ) );

	private static IEnumerable<SyntaxToken> TransformAllCore(
		SyntaxTokenList input,
		Func<SyntaxToken, SyntaxToken?> transformer
	) {
		foreach( var node in input ) {
			var transformed = transformer( node );

			if( transformed.HasValue ) {
				yield return transformed.Value;
			}
		}
	}

	private static IEnumerable<U> TransformAllCore<T, U>(
		IEnumerable<T> input,
		Func<T, U?> transformer
	) where T : SyntaxNode
	  where U : SyntaxNode
	{
		foreach( var node in input ) {
			var transformed = transformer( node );

			if( transformed != null ) {
				yield return transformed;
			}
		}
	}

	protected static T? MaybeTransform<T>( T? input, Func<T, T> transform )
		where T : SyntaxNode
	{
		if( input is null ) {
			return null;
		}

		return transform( input );
	}
}

