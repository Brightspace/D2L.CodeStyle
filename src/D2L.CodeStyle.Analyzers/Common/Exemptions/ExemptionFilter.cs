using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Common.Exemptions {
	/// <summary>
	/// Filters a SyntaxNodeAction against a hard-coded list of exemptions.
	/// This can be used at the top (when registering a syntax node action) or
	/// at the bottom (e.g. when emitting a diagnostic) or anywhere in-between.
	/// </summary>
	internal sealed class ExemptionFilter {
		private readonly ImmutableHashSet<Exemption> m_exemptions;

		public ExemptionFilter(
			ImmutableHashSet<Exemption> exemptions
		) {
			m_exemptions = exemptions;
		}

		public Action<SyntaxNodeAnalysisContext> Apply(
			Action<SyntaxNodeAnalysisContext> analyzer
		) {
			return ctx => {
				if( !AnExemptionApplies( ctx ) ) {
					analyzer( ctx );
				}
			};
		}

		private bool AnExemptionApplies( SyntaxNodeAnalysisContext context ) {
			var possibleReasonsToExempt = GetPossibleExemptions( context );
			return m_exemptions.Intersect( possibleReasonsToExempt ).Any();
		}

		private static IEnumerable<Exemption> GetPossibleExemptions( SyntaxNodeAnalysisContext context ) {
			yield return new Exemption(
				ExemptionKind.Assembly,
				context.Compilation.AssemblyName
			);

			var model = context.SemanticModel;
			var node = context.Node;

			while( node != null ) {
				if ( node is CompilationUnitSyntax ) {
					yield break;
				}

				if( node is MethodDeclarationSyntax ) {
					yield return CreateExemptionFromSymbol(
						ExemptionKind.Method,
						model,
						node
					);
				} else if( node is ConstructorDeclarationSyntax ) {
					yield return CreateExemptionFromSymbol(
						ExemptionKind.Method, // constructors are treated like methods
						model,
						node
					);
				} else if( node is ClassDeclarationSyntax ) {
					yield return CreateExemptionFromSymbol(
						ExemptionKind.Class,
						model,
						node
					);
				} else if( node is StructDeclarationSyntax ) {
					yield return CreateExemptionFromSymbol(
						ExemptionKind.Class, // structs are treated like classes
						model,
						node
					);
				}

				node = node.Parent;
			}
		}

		private static Exemption CreateExemptionFromSymbol(
			ExemptionKind kind,
			SemanticModel model,
			SyntaxNode node
		) {
			// This helper method is only useful when GetDeclaredSymbol makes
			// sense. New exemption types may not be able to use it.
			var symbol = model.GetDeclaredSymbol( node );

			return new Exemption(
				kind,
				identifier: symbol.ToString()
			);
		}
	}
}
