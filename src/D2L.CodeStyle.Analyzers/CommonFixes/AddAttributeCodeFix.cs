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
		public const string USING_STATIC_ARG = "UsingStatic";
		public const string USING_NAMESPACE_ARG = "Using";
		public const string ATTRIBUTE_NAME_ARG = "Attr";

		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create(
				Diagnostics.MissingTransitiveImmutableAttribute.Id
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
				var identifierSpan = diagnostic.Location.SourceSpan;

				var decl = root.FindNode( identifierSpan ) as TypeDeclarationSyntax;

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
			TypeDeclarationSyntax decl,
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
				if ( u.StaticKeyword == null && usingStatic ) {
					return false;
				}

				if ( u.StaticKeyword != null && !usingStatic ) {
					return false;
				}

				// Using a string here is pretty lame but I think it's ok.
				return u.Name.ToString() == usingNs;
			}
		}
		
		private static TypeDeclarationSyntax AddAttribute(
			TypeDeclarationSyntax decl,
			string attrName
		) {
			var attrSyntax = CreateAttributeSyntax( attrName );

			var leadingTrivia = decl.GetLeadingTrivia();

			// Need to strip the trivia first before adding attribute lists
			// because if it doesn't have any attr lists now it won't be able
			// to remove the trivia from whatever the first token is when we
			// add an attribute list.
			decl = decl.WithoutLeadingTrivia();

			// TypeDeclarationSyntax (the base class of these 3 types) doesn't
			// have AddAttributeLists... so we need to copy+paste some code.

			if ( decl is ClassDeclarationSyntax cls ) {
				decl = cls.AddAttributeLists( attrSyntax );

			} else if ( decl is StructDeclarationSyntax st ) {
				decl = st.AddAttributeLists( attrSyntax );

			} else if ( decl is InterfaceDeclarationSyntax iface ) {
				decl = iface.AddAttributeLists( attrSyntax );

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

			if ( properties.ContainsKey( USING_STATIC_ARG ) ) {
				usingStatic = bool.Parse( properties[USING_STATIC_ARG] );
			}

			string usingNs = properties[USING_NAMESPACE_ARG];
			string attrName = properties[ATTRIBUTE_NAME_ARG];

			return (usingStatic, usingNs, attrName);
		}
	}
}
