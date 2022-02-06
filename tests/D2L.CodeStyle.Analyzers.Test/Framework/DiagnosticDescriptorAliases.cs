using System.Collections.Immutable;
using System.Reflection;
using D2L.CodeStyle.Analyzers;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Framework {

	internal static class DiagnosticDescriptorAliases {

		private static readonly ImmutableDictionary<string, string> m_idsToAliases;
		private static readonly ImmutableDictionary<string, DiagnosticDescriptor> m_aliasesToDescriptors;

		static DiagnosticDescriptorAliases() {

			var idsToAliases = ImmutableDictionary.CreateBuilder<string, string>();
			var aliasesToDescriptors = ImmutableDictionary.CreateBuilder<string, DiagnosticDescriptor>();

			FieldInfo[] fields = typeof( Diagnostics ).GetFields( BindingFlags.Public | BindingFlags.Static );
			foreach( FieldInfo field in fields ) {

				DiagnosticDescriptor descriptor = field.GetValue( null ) as DiagnosticDescriptor;
				if( descriptor == null ) {
					continue;
				}

				idsToAliases.Add( descriptor.Id, field.Name );
				aliasesToDescriptors.Add( field.Name, descriptor );
			}

			m_idsToAliases = idsToAliases.ToImmutable();
			m_aliasesToDescriptors = aliasesToDescriptors.ToImmutable();
		}

		public static bool TryGetAlias( string id, out string alias ) {
			return m_idsToAliases.TryGetValue( id, out alias );
		}

		public static bool TryGetDescriptor( string alias, out DiagnosticDescriptor descriptor ) {
			return m_aliasesToDescriptors.TryGetValue( alias, out descriptor );
		}
	}
}
