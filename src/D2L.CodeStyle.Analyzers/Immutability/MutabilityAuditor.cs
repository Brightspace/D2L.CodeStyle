using System.Linq;
using Microsoft.CodeAnalysis;
using static D2L.CodeStyle.Analyzers.Immutability.ImmutableDefinitionChecker;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class MutabilityAuditor {
		public static bool CheckAudited(
			ISymbol symbol,
			DiagnosticSink diagnosticSink,
			out Location location ) {

			// Collect audit information
			var hasStaticAudited = Attributes.Statics.Audited.IsDefined( symbol );
			var hasStaticUnaudited = Attributes.Statics.Unaudited.IsDefined( symbol );
			var hasMutabilityAudited = Attributes.Mutability.Audited.IsDefined( symbol );
			var hasMutabilityUnaudited = Attributes.Mutability.Unaudited.IsDefined( symbol );
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
				.GetSyntax()
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
			}

			// Everything looks good, so collect information to determine if
			// auditing is necessary
			AttributeData attr
				= Attributes.Statics.Audited.GetAll( symbol ).FirstOrDefault()
				?? Attributes.Statics.Unaudited.GetAll( symbol ).FirstOrDefault()
				?? Attributes.Mutability.Audited.GetAll( symbol ).FirstOrDefault()
				?? Attributes.Mutability.Unaudited.GetAll( symbol ).FirstOrDefault();

			if( attr != null ) {
				location = GetLocation( attr );
				return true;
			}
			location = null;
			return false;
		}

		private static Location GetLocation( AttributeData attr )
			=> attr.ApplicationSyntaxReference.GetSyntax().GetLocation();
	}
}
