using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
			internal static readonly RoslynAttribute ImmutableBaseClass = new RoslynAttribute( "D2L.CodeStyle.Annotations.Objects.ImmutableBaseClassAttribute" );
			internal static readonly RoslynAttribute ImmutableGeneric = new RoslynAttribute( "D2L.CodeStyle.Annotations.Objects.ImmutableGenericAttribute" );
		}
		internal static class Mutability {
			internal static readonly RoslynAttribute Audited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Mutability.AuditedAttribute" );
			internal static readonly RoslynAttribute Unaudited = new RoslynAttribute( "D2L.CodeStyle.Annotations.Mutability.UnauditedAttribute" );

			internal static bool IsDefined( ISymbol symbol ) {
				return Audited.IsDefined( symbol ) || Unaudited.IsDefined( symbol );
			}

			internal static IEnumerable<AttributeSyntax> GetAllAttributeSyntax(
				ISymbol symbol
			) {
				if( Audited.TryGetAttributeSyntax(symbol, out AttributeSyntax auditedSyntax ) ) {
					yield return auditedSyntax;
				}
				if( Unaudited.TryGetAttributeSyntax(symbol, out AttributeSyntax unauditedSyntax ) ) {
					yield return unauditedSyntax;
				}
			}

		}

		internal static readonly RoslynAttribute Singleton = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.SingletonAttribute" );
		internal static readonly RoslynAttribute DIFramework = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.DIFrameworkAttribute" );
		internal static readonly RoslynAttribute Dependency = new RoslynAttribute( "D2L.LP.Extensibility.Activation.Domain.DependencyAttribute" );

		internal sealed class RoslynAttribute {

			private readonly string m_fullTypeName;

			public RoslynAttribute( string fullTypeName ) {
				m_fullTypeName = fullTypeName;
			}

			internal bool TryGetAttributeSyntax(
				ISymbol symbol,
				out AttributeSyntax attrSyntax
			) {
				AttributeData attrData = GetAll( symbol ).FirstOrDefault();

				if( attrData == null ) {
					attrSyntax = null;
					return false;
				}

				SyntaxNode syntaxNode = attrData
					.ApplicationSyntaxReference?
					.GetSyntax();

				attrSyntax = syntaxNode as AttributeSyntax;

				return true;
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
