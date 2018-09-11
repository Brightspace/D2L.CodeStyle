using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityExceptionInheritanceAnalyzer : DiagnosticAnalyzer {
		public const string CODE_FIX_DATA_KEY = "excepts";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.ImmutableExceptionInheritanceIsInvalid
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			var immutableAttribute = context.Compilation
				.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.Objects+Immutable" );

			if ( immutableAttribute == null ) {
				// If we can't find the symbol then we (and any interface we
				// implement or base class, recursively) couldn't have
				// [Immutable] so we don't need to bother with analysis.
				return;
			}

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeTypeDeclaration( ctx, immutableAttribute ),
				SyntaxKind.ClassDeclaration,
				SyntaxKind.InterfaceDeclaration,
				SyntaxKind.StructDeclaration
			);
		}

		private void AnalyzeTypeDeclaration(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol immutableAttribute
		) {
			// TypeDeclarationSyntax is the base class of
			// ClassDeclarationSyntax and StructDeclarationSyntax
			var declSyntax = (TypeDeclarationSyntax)context.Node;

			var declType = context.SemanticModel
				.GetDeclaredSymbol( declSyntax );

			ImmutableHashSet<string> directExceptions;
			if( !declType.TryGetDirectImmutableExceptions( out directExceptions ) ) {
				// Type has no direct exceptions defined, so there's nothing to analyze
				return;
			}

			if ( directExceptions.IsEmpty ) {
				// If we don't allow any exceptions then we couldn't possibly
				// trigger this diagnostic.
				return;
			}

			// Check that all of our allowed exceptions are also allowed by our
			// super-types. Emit at most one diagnostic (mentioning one of the
			// more restrictive super-types) per declaration syntax.

			var allInheritedExceptions = declType.GetInheritedImmutableExceptions();

			// We start maximalExceptions with the values from
			// directExceptions. We only ever change it by intersecting it with
			// other sets which means it will never be bigger than
			// directExceptions. This is nice because we may suggest a fix which
			// involves setting the exceptions to this set, i.e. we will never
			// suggest to *add* new kinds of exceptions.
			var maximalExceptions = new HashSet<string>( directExceptions );

			ISymbol aSuperTypeWithFewerAllowedExceptions = null;

			foreach( var inheritedExceptions in allInheritedExceptions ) {
				if( !directExceptions.IsSubsetOf( inheritedExceptions.Value ) ) {
					aSuperTypeWithFewerAllowedExceptions = inheritedExceptions.Key;
					maximalExceptions.IntersectWith( inheritedExceptions.Value );
				}
			}

			if ( aSuperTypeWithFewerAllowedExceptions == null ) {
				// We didn't find anything wrong.
				return;
			}

			var location = GetImmutableAttributeSyntax(
				context.SemanticModel,
				immutableAttribute,
				declSyntax
			).GetLocation();

			var fixInfo = GetInfoForFix( maximalExceptions );

			var diagnostic = Diagnostic.Create(
				Diagnostics.ImmutableExceptionInheritanceIsInvalid,
				location,
				messageArgs: new[] { aSuperTypeWithFewerAllowedExceptions.Name },
				properties: fixInfo
			);

			context.ReportDiagnostic( diagnostic );
		}

		/// <summary>
		/// Format necessary context for the code fix
		/// </summary>
		/// <param name="maximalExceptions">The largest set of exception kinds
		/// we are allowed to have</param>
		/// <returns>Object that can be passed to the code fix</returns>
		private ImmutableDictionary<string, string> GetInfoForFix(
			IEnumerable<string> maximalExceptions
		) {
			var sortedMaximalExceptions = maximalExceptions
				.OrderBy( v => v );

			var serializedExceptions = string.Join(
				",",
				sortedMaximalExceptions
			);

			return ImmutableDictionary.Create<string, string>()
				.Add( CODE_FIX_DATA_KEY, serializedExceptions )
				.ToImmutableDictionary();
		}

		private static AttributeSyntax GetImmutableAttributeSyntax(
			SemanticModel model,
			INamedTypeSymbol immutableAttribute,
			TypeDeclarationSyntax decl
		) {
			var attrs = decl.AttributeLists
				.SelectMany( al => al.Attributes );

			foreach( var attr in attrs ) {
				var attrType = model.GetSymbolInfo( attr )
					.Symbol // the symbol for one of the attribute constructors
					.ContainingType; // the symbol for the attributes type

				if( attrType == immutableAttribute ) {
					return attr;
				}
			}

			// Not reached in practice
			throw new Exception( "Couldn't find the attribute" );
		}
	}
}
