#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.CommonFixes {
	// BUG: this doesn't consider multiple partial decls so if the attribute
	// has allowMultiple: false this fix will create compile errors if you 
	// run "Fix all" in VS.
	//
	// To fix it maybe we could make this fixer find all decl syntaxes
	// and use some method to pick one (e.g. look at the BaseList syntax,
	// ToString() it and pick the lowest lexicographically) and only suggest a
	// fix on that decl. VS will take care of de-duping these fixes in a
	// "Fix All".

	[ExportCodeFixProvider(
		LanguageNames.CSharp,
		Name = nameof( AddAttributeCodeFix )
	)]
	public sealed class AddAttributeCodeFix : CodeFixProvider {

		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.MissingTransitiveImmutableAttribute.Id,
				Diagnostics.BlockingCallersMustBeBlocking.Id,
				Diagnostics.NonBlockingImplementationOfBlockingThing.Id
			);

		public override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(
			CodeFixContext context
		) {
			var root = await context.Document
				.GetSyntaxRootAsync( context.CancellationToken )
				.ConfigureAwait( false ) as CompilationUnitSyntax;

			foreach( var diagnostic in context.Diagnostics ) {
				// Sometimes the diagnostic is on the declaration that needs the attribute,
				// if not it is the last "additional location" by convention.
				var location = diagnostic.AdditionalLocations?.LastOrDefault() ?? diagnostic.Location;

				var node = root.FindNode( location.SourceSpan );
				var decl = node as MemberDeclarationSyntax;

				if( decl == null ) {
					throw new NotImplementedException( $"Adding attributes to {node.Kind()} is not supported. This is a D2L.CodeStyle.Analyzers bug." );
				}

				(bool usingStatic, string usingNs, string attrName)
					= GetFixArgs( diagnostic.Properties );

				context.RegisterCodeFix(
					CodeAction.Create(
						title: $"Add [{attrName}]",
						createChangedDocument: ct => Fix(
							context.Document,
							root,
							decl,
							usingStatic: usingStatic,
							usingNs: usingNs,
							attrName: attrName,
							ct
						)
					),
					diagnostic
				);
			}
		}

		private static Task<Document> Fix(
			Document orig,
			CompilationUnitSyntax root,
			MemberDeclarationSyntax decl,
			bool usingStatic,
			string usingNs,
			string attrName,
			CancellationToken ct
		) {
			var newDecl = AddAttribute( decl, attrName );

			var newRoot = root.ReplaceNode( decl, newDecl );

			newRoot = AddUsingIfNecessary( newRoot, usingStatic, usingNs );

			var newDoc = orig.WithSyntaxRoot( newRoot );

			return Task.FromResult( newDoc );
		}


		private static CompilationUnitSyntax AddUsingIfNecessary(
			CompilationUnitSyntax root,
			bool usingStatic,
			string usingNs
		) {
			if( root.Usings.Any( IsTheNecessaryUsingDirective ) ) {
				return root; // nothing to do
			}

			var usingDirective = CreateUsingDirectiveSyntax(
				usingStatic,
				usingNs
			);

			return root.WithUsings( root.Usings.Add( usingDirective ) );

			bool IsTheNecessaryUsingDirective( UsingDirectiveSyntax u ) {
				if ( u.StaticKeyword.IsKind( SyntaxKind.None ) && usingStatic ) {
					return false;
				}

				if ( !u.StaticKeyword.IsKind( SyntaxKind.None ) && !usingStatic ) {
					return false;
				}

				// Using a string here is pretty lame but I think it's ok.
				return u.Name.ToString() == usingNs;
			}
		}
		
		private static MemberDeclarationSyntax AddAttribute(
			MemberDeclarationSyntax decl,
			string attrName
		) {
			var attrSyntax = CreateAttributeSyntax( attrName );

			var leadingTrivia = decl.GetLeadingTrivia();

			// Need to strip the trivia first before adding attribute lists
			// because if it doesn't have any attr lists now it won't be able
			// to remove the trivia from whatever the first token is when we
			// add an attribute list.
			decl = decl.WithoutLeadingTrivia();

			// MemberDeclarationSyntax doesn't have AddAttributeLists... so we
			// need to copy+paste some code.

			if( decl is ClassDeclarationSyntax cls ) {
				decl = cls.AddAttributeLists( attrSyntax );

			} else if( decl is StructDeclarationSyntax st ) {
				decl = st.AddAttributeLists( attrSyntax );

			} else if( decl is InterfaceDeclarationSyntax iface ) {
				decl = iface.AddAttributeLists( attrSyntax );

			} else if( decl is MethodDeclarationSyntax method ) {
				decl = method.AddAttributeLists( attrSyntax );

			} else {
				throw new NotImplementedException();
			}

			// Re-add any leading trivia
			return decl.WithLeadingTrivia( leadingTrivia );
		}

		// Creates syntax for "[{attrName}]"
		private static AttributeListSyntax CreateAttributeSyntax(
			string attrName
		) {
			return SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.Attribute(
						SyntaxFactory.IdentifierName( attrName )
					)
				)
			);
		}

		// Creates syntax for "using static(?) {usingNs};"
		private static UsingDirectiveSyntax CreateUsingDirectiveSyntax(
			bool usingStatic,
			string usingNs
		) {
			var directive = SyntaxFactory.UsingDirective(
				SyntaxFactory.ParseName( usingNs )
			);

			if( usingStatic ) {
				directive = directive.WithStaticKeyword(
					SyntaxFactory.Token( SyntaxKind.StaticKeyword )
				);
			}

			return directive;
		}

		private static (bool, string, string) GetFixArgs(
			ImmutableDictionary<string, string> properties
		) {
			bool usingStatic = false;

			if ( properties.ContainsKey( AddAttributeCodeFixArgs.UsingStatic ) ) {
				usingStatic = bool.Parse( properties[ AddAttributeCodeFixArgs.UsingStatic ] );
			}

			string usingNs = properties[ AddAttributeCodeFixArgs.UsingNamespace ];
			string attrName = properties[ AddAttributeCodeFixArgs.AttributeName ];

			return (usingStatic, usingNs, attrName);
		}
	}
}
