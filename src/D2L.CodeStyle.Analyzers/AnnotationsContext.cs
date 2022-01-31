#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers {
	internal sealed class AnnotationsContext {

		public static bool IsAvailable( Compilation compilation ) => Build.MandatoryReferencesAnalyzer.HasAnnotationsReference( compilation );

		public static bool TryCreate( Compilation compilation, out AnnotationsContext ctx ) {
			if( IsAvailable( compilation ) ) {
				ctx = new AnnotationsContext( compilation );
				return true;
			}

			ctx = null;
			return false;
		}

		private AnnotationsContext( Compilation compilation ) {
			Statics = (
				Audited: GetAttr( compilation, "D2L.CodeStyle.Annotations.Statics+Audited" ),
				Unaudited: GetAttr( compilation, "D2L.CodeStyle.Annotations.Statics+Unaudited" )
			);
			Objects = (
				Immutable: GetAttr( compilation, "D2L.CodeStyle.Annotations.Objects+Immutable" ),
				ImmutableBaseClass: GetAttr( compilation, "D2L.CodeStyle.Annotations.Objects+ImmutableBaseClassAttribute" ),
				ConditionallyImmutable: GetAttr( compilation, "D2L.CodeStyle.Annotations.Objects+ConditionallyImmutable" ),
				OnlyIf: GetAttr( compilation, "D2L.CodeStyle.Annotations.Objects+ConditionallyImmutable+OnlyIf" )
			);
			Mutability = (
				Audited: GetAttr( compilation, "D2L.CodeStyle.Annotations.Mutability+AuditedAttribute" ),
				Unaudited: GetAttr( compilation, "D2L.CodeStyle.Annotations.Mutability+UnauditedAttribute" )
			);
		}

		internal (Attr Audited, Attr Unaudited) Statics { get; }
		internal (Attr Immutable, Attr ImmutableBaseClass, Attr ConditionallyImmutable, Attr OnlyIf) Objects { get; }
		internal (Attr Audited, Attr Unaudited) Mutability { get; }

		private static Attr GetAttr( Compilation compilation, string metadataName )
			=> new( compilation.GetTypeByMetadataName( metadataName ) );

		internal sealed class Attr {

			public Attr( INamedTypeSymbol symbol ) => Symbol = symbol;

			public INamedTypeSymbol Symbol { get; }

			internal ImmutableArray<AttributeData> GetAll( ISymbol s ) {
				return s.GetAttributes().Where( Matches ).ToImmutableArray();
			}

			internal bool IsDefined( ISymbol s ) {
				return s.GetAttributes().Any( Matches );
			}

			private bool Matches( AttributeData a ) => SymbolEqualityComparer.Default.Equals( a.AttributeClass, Symbol );

		}
	}
}
