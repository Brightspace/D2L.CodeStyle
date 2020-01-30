using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.UnnecessaryParameters {
	internal static class Methods {
		internal static readonly IReadOnlyDictionary<string, ImmutableArray<string>> Definitions =
			ImmutableDictionary.Create<string, ImmutableArray<string>>()
			.Add(
				 "D2L.Core.Security.D2LSecurity",
				 new[] {
					"HasPermission",
					"HasCapability"
				}
			);

		private static ImmutableDictionary<string, ImmutableArray<string>> Add(
				this ImmutableDictionary<string, ImmutableArray<string>> types,
				string typeFullName,
				string[] methodNames
			) {

			return types.Add(
				typeFullName,
				 methodNames.ToImmutableArray()
			);
		}
	}
}
