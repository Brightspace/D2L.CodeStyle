using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Async.Generator;

internal partial class SyncGenerator {
	/// <summary>
	/// Creates a source file containing generated methods and the necessary
	/// namespaces, using directives, and type declarations for them to live in
	/// (but only the necessary ones).
	/// </summary>
	internal sealed class FileCollector {
		private static readonly SyntaxToken NoneToken
			= SyntaxFactory.Token( SyntaxKind.None );

		private static readonly SyntaxTriviaList EmptyTrivia
			= SyntaxFactory.TriviaList();

		private readonly CompilationUnitSyntax m_root;

		// This is a mutable dictionary because we will remove from it as we go
		// and check that it is empty at the end, just to be safe.
		private readonly Dictionary<SyntaxNode, IEnumerable<string>> m_methods;

		/// <summary>
		/// Wraps the sort of syntax that we will append as part of a preamble, to
		/// defer calls to ToFullString()
		/// </summary>
		private readonly struct PieceOfSyntax {
			public object Data { get; init; }

			public static implicit operator PieceOfSyntax( SyntaxNode node)
				=> new() { Data = node };

			public static implicit operator PieceOfSyntax( SyntaxToken token )
				=> new() { Data = token };

			public static implicit operator PieceOfSyntax( SyntaxList<UsingDirectiveSyntax> list )
				=> new() { Data = list };

			public override string ToString() => Data switch {
				SyntaxNode n => n.ToFullString(),
				SyntaxToken t => t.ToFullString(),
				SyntaxList<UsingDirectiveSyntax> nl => nl.ToFullString(),
				_ => throw new BugException( "invalid piece of syntax " + Data.GetType().Name )
			};
		}

		// When we recurse into a namespace or type, we're not sure if the
		// preamble ("namespace foo {", "\t\tpartial class Bar<T> {", etc.) is
		// necessary, because maybe there are no generated methods.
		// Using a LinkedList<string> as a double-ended queue.
		private readonly LinkedList<PieceOfSyntax[]> m_preambles = new();

		private readonly StringBuilder m_out = new();

		private FileCollector(
			CompilationUnitSyntax root,
			Dictionary<SyntaxNode, IEnumerable<string>> methods
		) {
			m_root = root;
			m_methods = methods;
		}

		public static FileCollector Create(
			CompilationUnitSyntax root,
			ImmutableArray<(SyntaxNode Parent, string Source)> methods
		) {
			var groupedMethods = methods
				.GroupBy( static m => m.Parent )
				.ToDictionary(
					static g => g.Key,
					static g => g.Select( static m => m.Source )
				);

			return new FileCollector( root, groupedMethods );
		}

		public string CollectSource() {
			// File-scoped usings:
			m_out.Append( m_root.Usings.ToFullString() );

			WriteChildren( m_root );

			if( m_preambles.Count != 0 ) {
				throw new BugException( "left over preambles" );
			}

			if( m_methods.Count != 0 ) {
				throw new BugException( "left over methods" );
			}

			return m_out.ToString();
		}

		private bool WriteChildren( SyntaxNode node ) {
			// Early-out if we have no methods left to place.
			if( m_methods.Count == 0 ) {
				return false;
			}

			var preambleCountBefore = m_preambles.Count;

			bool wroteSomething = false;

			// First recurse into child types
			foreach( var child in node.ChildNodes() ) {
				bool wroteSomethingHere = child switch {
					ClassDeclarationSyntax @class => WriteType( @class ),
					StructDeclarationSyntax @struct => WriteType( @struct ),
					InterfaceDeclarationSyntax @interface => WriteType( @interface ),

					// Covers record + record struct:
					RecordDeclarationSyntax @record => WriteType( @record, @record.ClassOrStructKeyword ),

					// Covers file-scoped and block namespaces
					BaseNamespaceDeclarationSyntax @namespace => WriteNamespace( @namespace ),

					// Ignore any other child node
					_ => false
				};

				if( wroteSomethingHere ) {
					wroteSomething = true;
				}

				CheckPremableInvariant( wroteSomething, preambleCountBefore );
			}

			return wroteSomething;
		}

		private bool WriteNamespace( BaseNamespaceDeclarationSyntax @namespace ) {
			SyntaxToken? closeBrace;

			switch( @namespace ) {
				case FileScopedNamespaceDeclarationSyntax ns:
					m_preambles.AddLast(
						new PieceOfSyntax[] {
							ns.NamespaceKeyword,
							ns.Name,
							ns.SemicolonToken
						}
					);

					closeBrace = null;
					break;
				case NamespaceDeclarationSyntax ns:
					m_preambles.AddLast(
						new PieceOfSyntax[] {
							ns.NamespaceKeyword,
							ns.Name,
							ns.OpenBraceToken,
							ns.Usings
						}
					);

					closeBrace = ns.CloseBraceToken;
					break;
				default:
					throw new BugException( "unexpected namespace style: " + @namespace.Kind() );
			}

			bool wroteSomething = WriteChildren( @namespace );

			CloseBraceIfNecessary( wroteSomething, closeBrace );

			return wroteSomething;
		}

		private bool WriteType( TypeDeclarationSyntax decl )
			=> WriteType( decl, NoneToken );

		private bool WriteType(
			TypeDeclarationSyntax decl,
			SyntaxToken secondaryKeyword
		) {
			var preambleCountBefore = m_preambles.Count;

			// This should either get used inside a call to WriteMethods either
			// somewhere down the call to WriteChildren here or the direct call
			// to WriteMethods here, or it will be discarded inside
			// CloseBraceIfNecessary.
			AddPartialTypeToPreamble(
				decl,
				keyword1: decl.Keyword,
				// for "record struct" etc.
				keyword2: secondaryKeyword,
				identifier: decl.Identifier,
				typeParameterList: decl.TypeParameterList,
				constraintClauses: decl.ConstraintClauses,
				openBrace: decl.OpenBraceToken
			);

			var wroteSomethingInChild = WriteChildren( decl );
			var wroteMethodsForMe = WriteMethods( decl );

			var wroteSomething = wroteSomethingInChild || wroteMethodsForMe;

			CloseBraceIfNecessary( wroteSomething, decl.CloseBraceToken );

			CheckPremableInvariant( wroteSomething, preambleCountBefore );

			return wroteSomething;
		}

		private void AddPartialTypeToPreamble(
			SyntaxNode decl,
			SyntaxToken keyword1,
			SyntaxToken keyword2,
			SyntaxToken identifier,
			TypeParameterListSyntax? typeParameterList,
			SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
			SyntaxToken openBrace
		) {
			// Create the partial keyword and grab all leading trivia from the node
			var partialKeyword = CreatePartialKeyword( decl );

			// Clear any leading trivia from the keyword, if it exists.
			// Only the first token on the line should have leading trivia, and
			// we've moved that onto partialKeyword.
			// This could get ugly if someone did
			//
			//   sealed class
			//      Foo {
			keyword1 = keyword1.WithLeadingTrivia( EmptyTrivia );
			keyword2 = keyword2.WithLeadingTrivia( EmptyTrivia );
			identifier = identifier.WithLeadingTrivia( EmptyTrivia );

			m_preambles.AddLast(
				new PieceOfSyntax[] {
					partialKeyword,
					keyword1,
					keyword2,
					identifier,
					(typeParameterList != null ? typeParameterList : NoneToken ),
					openBrace
				}
			);
		}

		private static SyntaxToken CreatePartialKeyword( SyntaxNode decl )
			=> SyntaxFactory.Token( SyntaxKind.PartialKeyword )
				.WithLeadingTrivia( decl.GetLeadingTrivia() )
				.WithTrailingTrivia( SyntaxFactory.Space );

		private void CloseBraceIfNecessary( bool wroteSomething, SyntaxToken? closeBrace ) {
			if( wroteSomething ) {
				if( closeBrace.HasValue ) {
					m_out.Append( closeBrace.Value.ToFullString() );
				}
			} else {
				// Throw away the start of the decl that we never wrote
				m_preambles.RemoveLast();
			}
		}

		private bool WriteMethods( SyntaxNode parent ) {
			if( !m_methods.TryGetValue( parent, out var methods ) ) {
				return false;
			}

			m_methods.Remove( parent );

			// Flush any preambles in the queue
			while( m_preambles.Count != 0 ) {
				foreach( var thing in m_preambles.First() ) {
					m_out.Append( thing );
				}
				m_preambles.RemoveFirst();
			}

			foreach( var method in methods ) {
				m_out.Append( method );
			}

			return true;
		}

		// Inline assertions to catch some obvious bugs quickly
		private void CheckPremableInvariant( bool wroteSomething, int countBefore ) {
			if( wroteSomething && m_preambles.Count != 0 ) {
				throw new BugException( "Left over preambles" );
			}

			if( !wroteSomething && m_preambles.Count != countBefore ) {
				throw new BugException( "Incorrect number of preambles" );
			}
		}

		public sealed class BugException : Exception {
			private readonly string m_message;

			public BugException( string message ) {
				m_message = message;
			}

			public override string Message => $"Bug in {nameof( SyncGenerator )}: {m_message}";
		}
	}
}
