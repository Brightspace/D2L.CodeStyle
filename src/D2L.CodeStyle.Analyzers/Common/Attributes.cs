using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common {
	internal static class Attributes {

		internal static class Types {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Types.Audited" );
		}
		internal static class Members {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Members.Audited" );
		}
		internal static class Statics {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Statics.Audited" );
			internal static readonly RoslynAttribute Unaudited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Statics.Unadited" );
		}
		internal static class Objects {
			internal static readonly RoslynAttribute Immutable = new RoslynAttribute( "D2L.CodeStyle.Annotations.Objects.Immutable" );
		}

		internal sealed class RoslynAttribute {

			private readonly string m_fullTypeName;

			public RoslynAttribute( string fullTypeName ) {
				m_fullTypeName = fullTypeName;
			}

			internal ImmutableArray<AttributeData> GetAll( ISymbol s ) {
				var arr = ImmutableArray.CreateBuilder<AttributeData>();

				foreach( var attr in s.GetAttributes() ) {
					var attrFullTypeName = attr.AttributeClass.GetFullTypeName();
					if( attrFullTypeName == m_fullTypeName ) {
						arr.Add( attr );
					}
				}

				return arr.ToImmutable();
			}

			internal AttributeData GetSingle( ISymbol s ) {
				return GetAll( s ).Single();
			}

			internal bool IsDefined( ISymbol s ) {
				foreach( var attr in s.GetAttributes() ) {
					var attrFullTypeName = attr.AttributeClass.GetFullTypeName();
					if( attrFullTypeName == m_fullTypeName ) {
						return true;
					}
				}
				return false;
			}

		}
	}
}
