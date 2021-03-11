using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers {
	internal static class Attributes {

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
