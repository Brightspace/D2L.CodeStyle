using System.Collections;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.Common.Exemptions {
	internal sealed class ExemptionFilterBuilder : IEnumerable {
		private readonly ImmutableHashSet<Exemption>.Builder m_builder = ImmutableHashSet.CreateBuilder<Exemption>();

		public ExemptionFilterBuilder Add( Exemption ex ) {
			m_builder.Add( ex );
			return this;
		}

		public IEnumerator GetEnumerator() {
			return m_builder.GetEnumerator();
		}

		public ExemptionFilter Build() {
			return new ExemptionFilter( m_builder.ToImmutableHashSet() );
		}
	}
}
