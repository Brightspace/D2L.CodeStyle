using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal partial class ImmutabilityContext {

		internal static readonly ImmutableArray<string> DefaultExtraTypes = ImmutableArray.Create(
			// Framework Container Types (not that the distinction matters)
			"System.Collections.Immutable.IImmutableSet`1",
			"System.Collections.Immutable.ImmutableArray`1",
			"System.Collections.Immutable.ImmutableDictionary`2",
			"System.Collections.Immutable.ImmutableHashSet`1",
			"System.Collections.Immutable.ImmutableList`1",
			"System.Collections.Immutable.ImmutableQueue`1",
			"System.Collections.Immutable.ImmutableSortedDictionary`2",
			"System.Collections.Immutable.ImmutableSortedSet`1",
			"System.Collections.Immutable.ImmutableStack`1",
			"System.Collections.Generic.KeyValuePair`2",
			"System.Collections.Generic.IReadOnlyCollection`1",
			"System.Collections.Generic.IReadOnlyList`1",
			"System.Collections.Generic.IReadOnlyDictionary`2",
			"System.Collections.Generic.IEnumerable`1",
			"System.Lazy`1",
			"System.Nullable`1",
			"System.Tuple`1",
			"System.Tuple`2",
			"System.Tuple`3",
			"System.Tuple`4",
			"System.Tuple`5",
			"System.Tuple`6",
			"System.Tuple`7",
			"System.Tuple`8",

			// Framework Types
			"System.ComponentModel.TypeConverter",
			"System.DateTime",
			"System.Drawing.Imaging.ImageFormat",
			"System.Drawing.Size", // only safe because it's a struct with primitive fields
			"System.Guid",
			"System.Net.IPNetwork",
			"System.Reflection.ConstructorInfo",
			"System.Reflection.FieldInfo",
			"System.Reflection.MemberInfo",
			"System.Reflection.MethodInfo",
			"System.Reflection.ParameterInfo",
			"System.Reflection.PropertyInfo",
			"System.Security.Cryptography.RNGCryptoServiceProvider",
			"System.String",
			"System.StringComparer",
			"System.Text.ASCIIEncoding",
			"System.Text.Encoding",
			"System.Text.RegularExpressions.Regex",
			"System.Text.UTF8Encoding",
			"System.Threading.ReaderWriterLockSlim",
			"System.TimeSpan",
			"System.Type",
			"System.Uri",
			"System.Version",
			"System.Web.UI.PageHandlerFactory",
			"System.Workflow.ComponentModel.DependencyProperty",
			"System.Xml.Serialization.XmlSerializer",
			"System.Xml.Linq.XName",
			"System.Xml.Linq.XNamespace",

			// 1st-Party Types
			"count4net.IRateCounter",
			"count4net.IStatCounter",
			"count4net.IValueCounter",
			"count4net.IDurationCounter",
			"count4net.Writers.DurationCounter",
			"D2L.LP.Utilities.DeferredInitializer`1",
			"D2L.LP.Extensibility.Activation.Domain.IPlugins`1",
			"D2L.LP.Extensibility.Activation.Domain.IPlugins`2",
			"D2L.LP.Extensibility.Plugins.IInstancePlugins`1",
			"D2L.LP.Extensibility.Plugins.IInstancePlugins`2",

			// 3rd-Party Types
			"Amazon.RegionEndpoint",
			"Amazon.S3.IAmazonS3",
			"log4net.ILog",
			"Newtonsoft.Json.JsonSerializer",
			"Newtonsoft.Json.JsonConverter",
			"System.IO.Abstractions.IFileSystem"
		);

		internal static ImmutabilityContext Create( Compilation compilation ) {
			var builder = ImmutableArray.CreateBuilder<ImmutableTypeInfo>( DefaultExtraTypes.Length );

			foreach( string typeName in DefaultExtraTypes ) {
				INamedTypeSymbol type = compilation.GetTypeByMetadataName( typeName );

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

	}
}
