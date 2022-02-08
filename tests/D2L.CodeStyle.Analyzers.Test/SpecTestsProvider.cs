using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using D2L.CodeStyle.SpecTests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers {

	internal static class SpecTestsProvider {

		public static IEnumerable<SpecTest> GetAll() {

			ImmutableArray<AdditionalText> additionalFiles = GetAdditionalFiles();
			ImmutableDictionary<string, DiagnosticDescriptor> diagnosticDescriptors = GetDiagnosticDescriptors();
			ImmutableArray<MetadataReference> metdataReferences = GetMetadataReferences();

			foreach( (string name, string source) in GetEmbeddedSpecTests() ) {

				yield return new SpecTest(
					Name: name,
					Source: source,
					AdditionalFiles: additionalFiles,
					MetadataReferences: metdataReferences,
					DiagnosticDescriptors: diagnosticDescriptors
				);
			}
		}

		private static ImmutableDictionary<string, DiagnosticDescriptor> GetDiagnosticDescriptors() {

			var builder = ImmutableDictionary.CreateBuilder<string, DiagnosticDescriptor>();

			foreach( FieldInfo field in typeof( Diagnostics ).GetFields() ) {

				if( field.GetValue( null ) is DiagnosticDescriptor diagnosticDescriptor ) {
					builder.Add( field.Name, diagnosticDescriptor );
				}
			}

			return builder.ToImmutable();
		}

		private static ImmutableArray<MetadataReference> GetMetadataReferences() {

			var builder = ImmutableArray.CreateBuilder<MetadataReference>();

			builder.AddAssemblyOf( typeof( object ) );                  // mscorlib
			builder.AddAssemblyOf( typeof( Uri ) );                     // System
			builder.AddAssemblyOf( typeof( Enumerable ) );              // System.Core
			builder.AddAssemblyOf( typeof( Annotations.Because ) );     // D2L.CodeStyle.Annotations

			Assembly systemRuntime = Assembly.Load( "System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" );
			builder.Add( MetadataReference.CreateFromFile( systemRuntime.Location ) );

			return builder.ToImmutable();
		}

		private static void AddAssemblyOf( this ImmutableArray<MetadataReference>.Builder builder, Type type ) {
			MetadataReference reference = MetadataReference.CreateFromFile( type.Assembly.Location );
			builder.Add( reference );
		}

		private static ImmutableArray<AdditionalText> GetAdditionalFiles() {

			var builder = ImmutableArray.CreateBuilder<AdditionalText>();

			Assembly testAssembly = Assembly.GetExecutingAssembly();
			foreach( string resourcePath in testAssembly.GetManifestResourceNames() ) {

				if( !resourcePath.EndsWith( "AllowedList.txt" ) ) {
					continue;
				}

				string virtualPath = Regex.Replace(
					resourcePath,
					@"^.*\.(?<allowedListName>[^\.]*)\.txt$",
					@"${allowedListName}.txt"
				);

				string text = ReadEmbeddedResourceAsString( testAssembly, resourcePath );

				builder.Add( new AdditionalFile( virtualPath, text ) );
			}

			return builder.ToImmutable();
		}

		private static IEnumerable<(string SpecName, string Source)> GetEmbeddedSpecTests() {

			Assembly assembly = Assembly.GetExecutingAssembly();

			foreach( string specFilePath in assembly.GetManifestResourceNames() ) {

				if( !specFilePath.EndsWith( ".cs" ) ) {
					continue;
				}

				// The file foo/bar.baz.cs has specName bar.baz
				string specName = Regex.Replace(
					specFilePath,
					@"^.*\.(?<specName>[^\.]*)\.cs$",
					@"${specName}"
				);

				string source = ReadEmbeddedResourceAsString( assembly, specFilePath );

				yield return (specName, source);
			}
		}

		private sealed class AdditionalFile : AdditionalText {

			private readonly string m_text;

			public AdditionalFile(
				string path,
				string text
			) {
				Path = path;
				m_text = text;
			}

			public override string Path { get; }
			public override SourceText GetText( CancellationToken cancellationToken = default ) => SourceText.From( m_text, Encoding.UTF8 );
		}

		private static string ReadEmbeddedResourceAsString( Assembly assembly, string name ) {

			using( Stream stream = assembly.GetManifestResourceStream( name ) )
			using( StreamReader specStream = new( stream ) ) {
				return specStream.ReadToEnd();
			}
		}
	}
}
