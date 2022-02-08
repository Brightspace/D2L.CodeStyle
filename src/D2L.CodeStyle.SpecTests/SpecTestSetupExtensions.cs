using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests {

	public static class SpecTestSetupExtensions {

		public static void AddAssembly( this ImmutableArray<MetadataReference>.Builder builder, string assemblyString ) {
			Assembly assembly = Assembly.Load( assemblyString );
			MetadataReference reference = MetadataReference.CreateFromFile( assembly.Location );
			builder.Add( reference );
		}

		public static void AddAssemblyOf( this ImmutableArray<MetadataReference>.Builder builder, Type type ) {
			MetadataReference reference = MetadataReference.CreateFromFile( type.Assembly.Location );
			builder.Add( reference );
		}

		public static string ReadEmbeddedResourceAsString( this Assembly assembly, string resourceName ) {
			using Stream stream = assembly.GetManifestResourceStream( resourceName );
			using StreamReader reader = new( stream );
			return reader.ReadToEnd();
		}
	}
}
