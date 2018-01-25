using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMethodUsages {
	internal static class DangerousMethods {
		internal static readonly IReadOnlyDictionary<string, ImmutableArray<string>> Definitions =
			ImmutableDictionary.Create<string, ImmutableArray<string>>()
			.Add<FieldInfo>(
				nameof( FieldInfo.SetValue ),
				nameof( FieldInfo.SetValueDirect )
			)
			.Add<PropertyInfo>(
				nameof( PropertyInfo.SetValue )
			)
			.Add<Task>(
				nameof( Task.Run )
			);

		private static ImmutableDictionary<string, ImmutableArray<string>> Add<T>(
				this ImmutableDictionary<string, ImmutableArray<string>> types,
				params string[] methodNames
			) {

			return types.Add(
				typeof( T ).FullName,
				ImmutableArray.Create( methodNames )
			);
		}
	}
}
