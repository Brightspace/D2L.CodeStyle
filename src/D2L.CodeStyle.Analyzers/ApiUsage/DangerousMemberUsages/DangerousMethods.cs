using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;

namespace D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages {

	internal static class DangerousMethods {

		internal static readonly IReadOnlyDictionary<string, ImmutableArray<string>> Definitions =
			ImmutableDictionary.Create<string, ImmutableArray<string>>()
			.Add<FieldInfo>(
				nameof( FieldInfo.SetValue ),
				"SetValueDirect"
			)
			.Add<PropertyInfo>(
				nameof( PropertyInfo.SetValue )
			)
			.Add<Task>(
				nameof( Task.Run )
			)
			.AddMethod(
				nameof( TaskFactory.StartNew ),
				new[] {
					typeof( TaskFactory ).FullName,
					typeof( TaskFactory<> ).FullName
				}
			)
			.AddMethod(
				"MapPath",
				new[] {
					"System.Web.HttpServerUtility",
					"System.Web.HttpServerUtilityBase",
					"System.Web.HttpServerUtilityWrapper",
					"System.Web.HttpRequest",
					"System.Web.HttpRequestBase",
					"System.Web.HttpRequestWrapper",
					"System.Web.HttpWorkerRequest",
					"System.Web.Hosting.HostingEnvironment",
					"System.Web.UI.Page",
					"System.Web.UI.UserControl",
					"System.Web.Configuration.UserMapPath"
				}
			)

			.AddMethod( 
				"Transfer",
				new[] {
					"System.Web.HttpServerUtility",
					"System.Web.HttpServerUtilityBase",
					"System.Web.HttpServerUtilityWrapper"
				}
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

		private static ImmutableDictionary<string, ImmutableArray<string>> AddMethod(
			this ImmutableDictionary<string, ImmutableArray<string>> types,
			string methodName,
			string[] containingTypeFullNames
		) {

			foreach( string type in containingTypeFullNames ) {

				List<string> methods = new List<string>( new[] { methodName } );

				if( types.ContainsKey( type ) ) {
					methods.AddRange( types[ type ] );
					types = types.Remove( type );
				}

				types = types.Add( type, methods.ToImmutableArray() );
			}

			return types;
		}
	}
}
