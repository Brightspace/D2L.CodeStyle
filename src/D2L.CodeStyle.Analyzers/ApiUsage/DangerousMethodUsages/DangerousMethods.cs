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
			)
			.AddMapPathMethod( "System.Web.HttpServerUtility" )
			.AddMapPathMethod( "System.Web.HttpServerUtilityBase" )
			.AddMapPathMethod( "System.Web.HttpServerUtilityWrapper" )
			.AddMapPathMethod( "System.Web.HttpRequest" )
			.AddMapPathMethod( "System.Web.HttpRequestBase" )
			.AddMapPathMethod( "System.Web.HttpRequestWrapper" )
			.AddMapPathMethod( "System.Web.HttpWorkerRequest" )
			.AddMapPathMethod( "System.Web.Hosting.HostingEnvironment" )
			.AddMapPathMethod( "System.Web.UI.Page" )
			.AddMapPathMethod( "System.Web.UI.UserControl" )
			.AddMapPathMethod( "System.Web.Configuration.UserMapPath" );

		private static ImmutableDictionary<string, ImmutableArray<string>> Add<T>(
				this ImmutableDictionary<string, ImmutableArray<string>> types,
				params string[] methodNames
			) {

			return types.Add(
				typeof( T ).FullName,
				ImmutableArray.Create( methodNames )
			);
		}

		/// <remarks>
		/// Type parameter is a string on purpose, to avoid having to add a reference to System.Web.
		/// Referencing it would prevent us from shipping a netstandard/.NET core build of the analyzers.
		/// </remarks>
		private static ImmutableDictionary<string, ImmutableArray<string>> AddMapPathMethod(
			this ImmutableDictionary<string, ImmutableArray<string>> types,
			string containingTypeFullName
		) {

			return types.Add( containingTypeFullName, ImmutableArray.Create( "MapPath" ) );
		}
	}
}
