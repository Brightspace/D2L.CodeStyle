using System.Collections.Generic;
using System.Collections.Immutable;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages {

	internal static class DangerousProperties {

		internal static readonly IReadOnlyDictionary<string, ImmutableArray<string>> Definitions =
			ImmutableDictionary.Create<string, ImmutableArray<string>>()
			.Add(
				"System.Net.ServicePointManager",
				ImmutableArray.Create(
					"CertificatePolicy",
					"CheckCertificateRevocationList",
					"DefaultConnectionLimit",
					"DnsRefreshTimeout",
					"EnableDnsRoundRobin",
					"EncryptionPolicy",
					"Expect100Continue",
					"MaxServicePointIdleTime",
					"MaxServicePoints",
					"ReusePort",
					"SecurityProtocol",
					"ServerCertificateValidationCallback",
					"UseNagleAlgorithm"
				)
			)
			.Add(
				"System.Reflection.AssemblyName",
				ImmutableArray.Create(
					"Version"
				)
			)
			.Add(
				"System.Web.HttpContext",
				ImmutableArray.Create(
					"Current",
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
	}
}