using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class MutabilityAuditor {
		public static bool IsAudited( ISymbol symbol, out Location location, out Diagnostic diagnostic ) {
			AttributeData attr;
			diagnostic = null;

			// Check if there is a conflicting audit AND unaudit
			if( ( Attributes.Statics.Audited.IsDefined( symbol ) || Attributes.Mutability.Audited.IsDefined( symbol ) )
				&& ( Attributes.Statics.Unaudited.IsDefined( symbol ) || Attributes.Mutability.Unaudited.IsDefined( symbol ) ) ) {
				diagnostic = Diagnostic.Create(
					Diagnostics.ConflictingAuditing,
					symbol.DeclaringSyntaxReferences[0].GetSyntax().GetLastToken().GetLocation() );
			}

			if( symbol.IsStatic ) {
				attr = Attributes.Statics.Audited.GetAll( symbol ).FirstOrDefault()
					?? Attributes.Statics.Unaudited.GetAll( symbol ).FirstOrDefault();
			} else {
				attr = Attributes.Mutability.Audited.GetAll( symbol ).FirstOrDefault()
					?? Attributes.Mutability.Unaudited.GetAll( symbol ).FirstOrDefault();
			}

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
