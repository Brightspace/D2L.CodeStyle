using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal partial class ImmutabilityContext {

		internal static readonly ImmutableArray<(string TypeName, string AssmeblyName)> DefaultExtraTypes = ImmutableArray.Create(
			// Framework Container Types (not that the distinction matters)
			("System.Collections.Immutable.IImmutableSet`1", default),
			("System.Collections.Immutable.ImmutableArray`1", default),
			("System.Collections.Immutable.ImmutableDictionary`2", default),
			("System.Collections.Immutable.ImmutableHashSet`1", default),
			("System.Collections.Immutable.ImmutableList`1", default),
			("System.Collections.Immutable.ImmutableQueue`1", default),
			("System.Collections.Immutable.ImmutableSortedDictionary`2", default),
			("System.Collections.Immutable.ImmutableSortedSet`1", default),
			("System.Collections.Immutable.ImmutableStack`1", default),
			("System.Collections.Generic.KeyValuePair`2", default),
			("System.Collections.Generic.IReadOnlyCollection`1", default),
			("System.Collections.Generic.IReadOnlyList`1", default),
			("System.Collections.Generic.IReadOnlyDictionary`2", default),
			("System.Collections.Generic.IEnumerable`1", default),
			("System.Lazy`1", default),
			("System.Nullable`1", default),
			("System.Tuple`1", default),
			("System.Tuple`2", default),
			("System.Tuple`3", default),
			("System.Tuple`4", default),
			("System.Tuple`5", default),
			("System.Tuple`6", default),
			("System.Tuple`7", default),
			("System.Tuple`8", default),

			// Framework Types
			("System.ComponentModel.TypeConverter", default),
			("System.DateTime", default),
			("System.Drawing.Imaging.ImageFormat", default),
			("System.Drawing.Size", default), // only safe because it's a struct with primitive fields
			("System.Guid", default),
			("System.Net.IPNetwork", default),
			("System.Reflection.ConstructorInfo", default),
			("System.Reflection.FieldInfo", default),
			("System.Reflection.MemberInfo", default),
			("System.Reflection.MethodInfo", default),
			("System.Reflection.ParameterInfo", default),
			("System.Reflection.PropertyInfo", default),
			("System.Security.Cryptography.RNGCryptoServiceProvider", default),
			("System.String", default),
			("System.StringComparer", default),
			("System.Text.ASCIIEncoding", default),
			("System.Text.Encoding", default),
			("System.Text.RegularExpressions.Regex", default),
			("System.Text.UTF8Encoding", default),
			("System.Threading.ReaderWriterLockSlim", default),
			("System.TimeSpan", default),
			("System.Type", default),
			("System.Uri", default),
			("System.Version", default),
			("System.Web.UI.PageHandlerFactory", default),
			("System.Workflow.ComponentModel.DependencyProperty", default),
			("System.Xml.Serialization.XmlSerializer", default),
			("System.Xml.Linq.XName", default),
			("System.Xml.Linq.XNamespace", default),

			// 1st-Party Types
			("count4net.IRateCounter", default),
			("count4net.IStatCounter", default),
			("count4net.IValueCounter", default),
			("count4net.IDurationCounter", default),
			("count4net.Writers.DurationCounter", default),
			("D2L.LP.Utilities.DeferredInitializer`1", default),
			("D2L.LP.Extensibility.Activation.Domain.IPlugins`1", default),
			("D2L.LP.Extensibility.Activation.Domain.IPlugins`2", default),
			("D2L.LP.Extensibility.Plugins.IInstancePlugins`1", default),
			("D2L.LP.Extensibility.Plugins.IInstancePlugins`2", default),

			// 3rd-Party Types
			("Amazon.RegionEndpoint", default),
			("Amazon.S3.IAmazonS3", default),
			("log4net.ILog", default),
			("Newtonsoft.Json.JsonSerializer", "Newtonsoft.Json"),
			("Newtonsoft.Json.JsonConverter", "Newtonsoft.Json"),
			("System.IO.Abstractions.IFileSystem", default)
		);

		internal static ImmutabilityContext Create( Compilation compilation ) {
			ImmutableDictionary<string, IAssemblySymbol> compilationAssmeblies = GetCompilationAssemblies( compilation );

			var builder = ImmutableArray.CreateBuilder<ImmutableTypeInfo>( DefaultExtraTypes.Length );

			foreach( ( string typeName, string qualifiedAssembly ) in DefaultExtraTypes ) {
				INamedTypeSymbol type;
				if( string.IsNullOrEmpty( qualifiedAssembly ) ) {
					type = compilation.GetTypeByMetadataName( typeName );
				} else {
					if( !compilationAssmeblies.TryGetValue( qualifiedAssembly, out IAssemblySymbol assembly ) ) {
						continue;
					}

					type = assembly.GetTypeByMetadataName( typeName );
				}

				if( type == null || type.Kind == SymbolKind.ErrorType ) {
					continue;
				}

				ImmutableTypeInfo info = ImmutableTypeInfo.CreateWithAllImmutableTypeParameters(
					ImmutableTypeKind.Total,
					type
				);

				builder.Add( info );
			}

			return new ImmutabilityContext( builder.ToImmutable() );
		}

		private static ImmutableDictionary<string, IAssemblySymbol> GetCompilationAssemblies( Compilation compilation ) {
			var builder = ImmutableDictionary.CreateBuilder<string, IAssemblySymbol>();

			IAssemblySymbol compilationAssmebly = compilation.Assembly;

			builder.Add( compilationAssmebly.Name, compilationAssmebly );

			foreach( IModuleSymbol module in compilationAssmebly.Modules ) {
				foreach( IAssemblySymbol assembly in module.ReferencedAssemblySymbols ) {
					builder.Add( assembly.Name, assembly );
				}
			}

			return builder.ToImmutable();
		}

	}
}
