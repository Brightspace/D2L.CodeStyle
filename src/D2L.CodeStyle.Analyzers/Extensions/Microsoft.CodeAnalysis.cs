using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Extensions {
	internal static partial class Extensions {
		/// <summary>
		/// A list of marked immutable types owned externally.
		/// </summary>
		private static readonly ImmutableHashSet<string> MarkedImmutableTypes = ImmutableHashSet.Create(
			"System.StringComparer",
			"System.Text.ASCIIEncoding",
			"System.Text.Encoding",
			"System.Text.UTF8Encoding"
		);
		
		public static bool IsTypeMarkedImmutable( this ITypeSymbol symbol ) {
			if( MarkedImmutableTypes.Contains( symbol.GetFullTypeName() ) ) {
				return true;
			}
			if( Attributes.Objects.Immutable.IsDefined( symbol ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedImmutable ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedImmutable( symbol.BaseType ) ) {
				return true;
			}
			return false;
		}

		public static bool TryGetImmutableAttributeData( this ITypeSymbol symbol, out AttributeData attrData ) {
			attrData = Attributes.Objects.Immutable.GetAll( symbol ).FirstOrDefault();
			if( attrData != null ) {
				return true;
			}
			foreach( INamedTypeSymbol interfaceSymbol in symbol.Interfaces ) {
				if( interfaceSymbol.TryGetImmutableAttributeData( out attrData ) ) {
					return true;
				}
			}
			if( symbol.BaseType == null ) {
				return false;
			}
			return symbol.BaseType.TryGetImmutableAttributeData( out attrData );
		}

		public static bool IsTypeMarkedSingleton( this ITypeSymbol symbol ) {
			if( Attributes.Singleton.IsDefined( symbol ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedSingleton ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedSingleton( symbol.BaseType ) ) {
				return true;
			}
			return false;
		}
	}
}
