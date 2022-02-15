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
			//Bazel caching requires knowledge of inputs, and verification that they haven't changed. If inputs have changed, it does not use the cache.
			//The usage of the assembly version number property prevents Bazel from caching because on each CI build, the version number is different
			//resulting in Bazel never using the cache. 
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
					"Handler",
					"Items"
				)
			).Add(
				"System.Web.HttpContextBase",
				ImmutableArray.Create(
					"Items"
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
