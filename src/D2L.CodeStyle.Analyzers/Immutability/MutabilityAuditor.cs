#nullable disable

using Microsoft.CodeAnalysis;
using static D2L.CodeStyle.Analyzers.Immutability.ImmutableDefinitionChecker;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class MutabilityAuditor {
		public static bool CheckAudited(
			AnnotationsContext annotationsContext,
			ISymbol symbol,
			DiagnosticSink diagnosticSink,
			CancellationToken cancellationToken,
			out Location location
		) {

			// Collect audit information
			var hasStaticAudited = annotationsContext.Statics_Audited.IsDefined( symbol );
			var hasMutabilityAudited = annotationsContext.Mutability_Audited.IsDefined( symbol );

			// If there are no audits, don't do anything
			if( !hasStaticAudited && !hasMutabilityAudited ) {
				location = null;
				return false;
			}

			var syntaxLocation = symbol
				.DeclaringSyntaxReferences[0]
				.GetSyntax( cancellationToken )
				.GetLastToken()
				.GetLocation();

			AttributeData attr = null;

			if( symbol.IsStatic ) {
				// Check if a static member is using mutability audits
				if( hasMutabilityAudited ) {
					var diagnostic = Diagnostic.Create(
						Diagnostics.InvalidAuditType,
						syntaxLocation,
						"static",
						symbol.Kind.ToString().ToLower(),
						"Statics.*" );
					diagnosticSink( diagnostic );
				}

				attr = annotationsContext.Statics_Audited.GetAll( symbol ).FirstOrDefault();
			} else {
				// Check if a non-static member is using static audits
				if( hasStaticAudited ) {
					var diagnostic = Diagnostic.Create(
						Diagnostics.InvalidAuditType,
						syntaxLocation,
						"non-static",
						symbol.Kind.ToString().ToLower(),
						"Mutability.*" );
					diagnosticSink( diagnostic );
				}

				attr = annotationsContext.Mutability_Audited.GetAll( symbol ).FirstOrDefault();
			}

			if( attr != null ) {
				location = GetLocation( attr, cancellationToken );
				return true;
			}
			location = null;
			return false;
		}

		private static Location GetLocation( AttributeData attr, CancellationToken cancellationToken )
			=> attr.ApplicationSyntaxReference.GetSyntax( cancellationToken ).GetLocation();
	}
}
