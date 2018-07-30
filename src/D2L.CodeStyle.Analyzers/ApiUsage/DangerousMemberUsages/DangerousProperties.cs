using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Reflection;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages {

	internal static class DangerousProperties {

		internal static readonly IReadOnlyDictionary<string, ImmutableArray<string>> Definitions =
			ImmutableDictionary.Create<string, ImmutableArray<string>>()
			.AddAllStaticProperties<ServicePointManager>()
			.Add(
				"System.Web.HttpContext",
				ImmutableArray.Create(
					"CurrentHandler",
					"Handler"
				)
			);

		private static ImmutableDictionary<string, ImmutableArray<string>> Add<T>(
				this ImmutableDictionary<string, ImmutableArray<string>> types,
				params string[] propertyNames
			) {

			return types.Add(
				typeof( T ).FullName,
				ImmutableArray.Create( propertyNames )
			);
		}

		private static ImmutableDictionary<string, ImmutableArray<string>> AddAllStaticProperties<T>(
				this ImmutableDictionary<string, ImmutableArray<string>> types
			) {

			PropertyInfo[] properties = typeof( T ).GetProperties( BindingFlags.Public | BindingFlags.Static );
			string[] propertyNames = properties.Select( p => p.Name ).ToArray();

			return types.Add<T>( propertyNames );
		}
	}
}
