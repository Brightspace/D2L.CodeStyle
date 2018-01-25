using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers {
	internal static class Attributes {
		internal static class Types {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Types.Audited" );
		}
		internal static class Members {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Members.Audited" );
		}
		internal static class Statics {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Statics.Audited" );
			internal static readonly RoslynAttribute Unaudited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Statics.Unaudited" );
		}
		internal static class Objects {
			internal static readonly RoslynAttribute Immutable = new RoslynAttribute( "D2L.CodeStyle.Annotations.Objects.Immutable" );
		}
		internal static class Mutability {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Mutability.AuditedAttribute" );
			internal static readonly RoslynAttribute Unaudited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Mutability.UnauditedAttribute" );
		}
		internal static readonly RoslynAttribute Singleton = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.SingletonAttribute" );
		internal static readonly RoslynAttribute DIFramework = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.DIFrameworkAttribute" );
		internal static readonly RoslynAttribute Dependency = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.DependencyAttribute" );

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
