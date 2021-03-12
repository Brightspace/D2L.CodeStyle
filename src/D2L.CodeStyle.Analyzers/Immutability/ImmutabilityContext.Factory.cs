using System.Collections.Immutable;
using System.Linq;
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
			("System.Index", default),
			("System.Net.IPNetwork", default),
			("System.Net.Http.HttpMethod", default),
			("System.Range", default),
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

			// 3rd-Party Types
			("Amazon.RegionEndpoint", default),
			("Amazon.S3.IAmazonS3", default),
			("log4net.ILog", default),
			("Newtonsoft.Json.JsonSerializer", "Newtonsoft.Json"),
			("Newtonsoft.Json.JsonConverter", "Newtonsoft.Json"),
			("System.IO.Abstractions.IFileSystem", default),
			("System.IO.Abstractions.FileSystem", default)
		);


		internal static readonly ImmutableArray<(string TypeName, string MethodName, string AssemblyName)> KnownImmutableReturningMethods = ImmutableArray.Create(
			( "System.Array", "Empty", default(string) ),
			( "System.Linq.Enumerable", "Empty", default(string) )
		);
		

		internal static ImmutabilityContext Create( Compilation compilation, AnnotationsContext annotationsContext ) {

			ImmutableDictionary<string, IAssemblySymbol> compilationAssemblies = GetCompilationAssemblies( compilation );

			// Generate a dictionary of types that we have specifically determined
			// should be considered Immutable by the Analyzer.
			var extraImmutableTypesBuilder = ImmutableDictionary.CreateBuilder<INamedTypeSymbol, ImmutableTypeInfo>();
			foreach( ( string typeName, string qualifiedAssembly ) in DefaultExtraTypes ) {
				INamedTypeSymbol type = GetTypeSymbol( compilationAssemblies, compilation, qualifiedAssembly, typeName );

				if( type == null ) {
					continue;
				}

				ImmutableTypeInfo info = ImmutableTypeInfo.CreateWithAllConditionalTypeParameters(
					ImmutableTypeKind.Total,
					type
				);

				extraImmutableTypesBuilder.Add( type, info );
			}

			// Generate a set of methods that we have specifically determined
			// have a return value which should be considered Immutable by the Analyzer.
			var knownImmutableReturnsBuilder = ImmutableHashSet.CreateBuilder<IMethodSymbol>();
			foreach( ( string typeName, string methodName, string qualifiedAssembly ) in KnownImmutableReturningMethods ) {
				INamedTypeSymbol type = GetTypeSymbol( compilationAssemblies, compilation, qualifiedAssembly, typeName );

				if( type == null ) {
					continue;
				}

				IMethodSymbol[] methodSymbol = type
					.GetMembers( methodName )
					.OfType<IMethodSymbol>()
					.Where( m => m.Parameters.Length == 0 )
					.ToArray();
					
				if( methodSymbol.Length != 1 ) {
					continue;
				}

				knownImmutableReturnsBuilder.Add( methodSymbol[0] );
			}

			return new ImmutabilityContext(
				annotationsContext: annotationsContext,
				extraImmutableTypes: extraImmutableTypesBuilder.ToImmutable(),
				knownImmutableReturns: knownImmutableReturnsBuilder.ToImmutable(),
				conditionalTypeParamemters: ImmutableHashSet<ITypeParameterSymbol>.Empty
			);
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

		private static INamedTypeSymbol GetTypeSymbol(
			ImmutableDictionary<string, IAssemblySymbol> compilationAssemblies,
			Compilation compilation,
			string qualifiedAssembly,
			string typeName
		) {
			INamedTypeSymbol type;

			if( string.IsNullOrEmpty( qualifiedAssembly ) ) {
				type = compilation.GetTypeByMetadataName( typeName );
			} else {
				if( !compilationAssemblies.TryGetValue( qualifiedAssembly, out IAssemblySymbol assembly ) ) {
					return null;
				}

				type = assembly.GetTypeByMetadataName( typeName );
			}

			if( type == null || type.Kind == SymbolKind.ErrorType ) {
				return null;
			}

			return type;
		}

	}
}
