using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers {

	internal static class Attributes {

		internal static readonly RoslynAttribute Singleton = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.SingletonAttribute" );
		internal static readonly RoslynAttribute DIFramework = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.DIFrameworkAttribute" );
		internal static readonly RoslynAttribute Dependency = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.DependencyAttribute" );
		internal static readonly RoslynAttribute Unlocatable = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.UnlocatableAttribute" );
		internal static readonly RoslynAttribute UnlocatableCandidate = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.Unlocatable.CandidateAttribute" );

		internal sealed class RoslynAttribute {

			private readonly string m_fullTypeName;

			public RoslynAttribute( string fullTypeName ) {
				m_fullTypeName = fullTypeName;
			}

			internal bool IsDefined( ISymbol type ) {

				foreach( AttributeData attr in type.GetAttributes() ) {

					INamedTypeSymbol? attributeClass = attr.AttributeClass;
					if( attributeClass.IsNullOrErrorType() ) {
						continue;
					}

					string attributeName = attributeClass.GetFullTypeName();
					if( attributeName.Equals( m_fullTypeName, StringComparison.Ordinal ) ) {
						return true;
					}
				}

				return false;
			}
		}
	}
}
