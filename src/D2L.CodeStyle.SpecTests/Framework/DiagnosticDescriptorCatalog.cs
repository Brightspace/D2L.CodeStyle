using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Framework {

	public sealed class DiagnosticDescriptorCatalog {

		private readonly ImmutableDictionary<string, DiagnosticDescriptor> m_aliasesToDescriptors;
		private readonly ImmutableDictionary<string, string> m_idsToAliases;

		private DiagnosticDescriptorCatalog(
				ImmutableDictionary<string, DiagnosticDescriptor> aliasesToDescriptors,
				ImmutableDictionary<string, string> idsToAliases
			) {

			m_aliasesToDescriptors = aliasesToDescriptors;
			m_idsToAliases = idsToAliases;
		}

		public bool TryGetAlias( string id, [NotNullWhen( true )] out string? alias ) {
			return m_idsToAliases.TryGetValue( id, out alias );
		}

		public bool TryGetDescriptor( string alias, [NotNullWhen( true )] out DiagnosticDescriptor? descriptor ) {
			return m_aliasesToDescriptors.TryGetValue( alias, out descriptor );
		}

		public static DiagnosticDescriptorCatalog Create( params Type[] types ) {

			var aliasesToDescriptors = ImmutableDictionary.CreateBuilder<string, DiagnosticDescriptor>();
			var idsToAliases = ImmutableDictionary.CreateBuilder<string, string>();

			IEnumerable<(string, DiagnosticDescriptor)> descriptors = types.SelectMany( GetDiagnosticDescriptors );
			foreach( (string alias, DiagnosticDescriptor descriptor) in descriptors ) {

				aliasesToDescriptors.Add( alias, descriptor );
				idsToAliases.Add( descriptor.Id, alias );
			}

			return new(
				aliasesToDescriptors: aliasesToDescriptors.ToImmutable(),
				idsToAliases: idsToAliases.ToImmutable()
			);
		}

		private static IEnumerable<(string, DiagnosticDescriptor)> GetDiagnosticDescriptors( Type type ) {

			FieldInfo[] fields = type.GetFields( BindingFlags.Public | BindingFlags.Static );
			foreach( FieldInfo field in fields ) {

				DiagnosticDescriptor? descriptor = field.GetValue( null ) as DiagnosticDescriptor;
				if( descriptor == null ) {
					continue;
				}

				yield return (field.Name, descriptor);
			}
		}

	}
}
