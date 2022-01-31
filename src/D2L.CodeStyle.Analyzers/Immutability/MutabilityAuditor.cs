#nullable disable

using System.Linq;
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
			var hasStaticAudited = annotationsContext.Statics.Audited.IsDefined( symbol );
			var hasStaticUnaudited = annotationsContext.Statics.Unaudited.IsDefined( symbol );
			var hasMutabilityAudited = annotationsContext.Mutability.Audited.IsDefined( symbol );
			var hasMutabilityUnaudited = annotationsContext.Mutability.Unaudited.IsDefined( symbol );
			var hasBothStaticsAttributes = hasStaticAudited && hasStaticUnaudited;
			var hasBothMutabilityAttributes = hasMutabilityAudited && hasMutabilityUnaudited;
			var hasEitherStaticsAttributes = hasStaticAudited || hasStaticUnaudited;
			var hasEitherMutabilityAttributes = hasMutabilityAudited || hasMutabilityUnaudited;

			// If there are no audits, don't do anything
			if( !hasEitherStaticsAttributes && !hasEitherMutabilityAttributes ) {
				location = null;
				return false;
			}

			var syntaxLocation = symbol
				.DeclaringSyntaxReferences[0]
				.GetSyntax( cancellationToken )
				.GetLastToken()
				.GetLocation();

			// Check if both static audits are applied
			if( hasBothStaticsAttributes ) {
				var diagnostic = Diagnostic.Create(
					Diagnostics.ConflictingImmutability,
					syntaxLocation,
					"Statics.Audited",
					"Statics.Unaudited",
					symbol.Kind.ToString().ToLower() );
				diagnosticSink( diagnostic );
			}

			// Check if both mutability audits are applied
			if( hasBothMutabilityAttributes ) {
				var diagnostic = Diagnostic.Create(
					Diagnostics.ConflictingImmutability,
					syntaxLocation,
					"Mutability.Audited",
					"Mutability.Unaudited",
					symbol.Kind.ToString().ToLower() );
				diagnosticSink( diagnostic );
			}

			AttributeData attr = null;

			if( symbol.IsStatic ) {
				// Check if a static member is using mutability audits
				if( hasEitherMutabilityAttributes ) {
					var diagnostic = Diagnostic.Create(
						Diagnostics.InvalidAuditType,
						syntaxLocation,
						"static",
						symbol.Kind.ToString().ToLower(),
						"Statics.*" );
					diagnosticSink( diagnostic );
				}

				attr = annotationsContext.Statics.Audited.GetAll( symbol ).FirstOrDefault()
					?? annotationsContext.Statics.Unaudited.GetAll( symbol ).FirstOrDefault();
			} else {
				// Check if a non-static member is using static audits
				if( hasEitherStaticsAttributes ) {
					var diagnostic = Diagnostic.Create(
						Diagnostics.InvalidAuditType,
						syntaxLocation,
						"non-static",
						symbol.Kind.ToString().ToLower(),
						"Mutability.*" );
					diagnosticSink( diagnostic );
				}

				attr = annotationsContext.Mutability.Audited.GetAll( symbol ).FirstOrDefault()
					?? annotationsContext.Mutability.Unaudited.GetAll( symbol ).FirstOrDefault();
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
