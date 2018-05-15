using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.UI;

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
			)
			.AddMapPathMethods();

		private static ImmutableDictionary<string, ImmutableArray<string>> AddMapPathMethods( 
				this ImmutableDictionary<string, ImmutableArray<string>> types 
			) {

			return types
				.Add<HttpServerUtility>(
					nameof( HttpServerUtility.MapPath ) 
				)
				.Add<HttpServerUtilityBase>(
					nameof( HttpServerUtilityBase.MapPath )
				)
				.Add<HttpServerUtilityWrapper>(
					nameof( HttpServerUtilityWrapper.MapPath )
				)
				.Add<HttpRequest>(
					nameof( HttpRequest.MapPath )
				)
				.Add<HttpRequestBase>(
					nameof( HttpRequestBase.MapPath )
				)
				.Add<HttpRequestWrapper>(
					nameof( HttpRequestWrapper.MapPath )
				)
				.Add<HttpWorkerRequest>(
					nameof( HttpWorkerRequest.MapPath )
				)
				.Add<HostingEnvironment>(
					nameof( HostingEnvironment.MapPath )
				)
				.Add<UserMapPath>(
					nameof( UserMapPath.MapPath )
				)
				.Add<Page>(
					nameof( Page.MapPath )
				)
				.Add<UserControl>(
					nameof( UserControl.MapPath )
				);
		}

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
