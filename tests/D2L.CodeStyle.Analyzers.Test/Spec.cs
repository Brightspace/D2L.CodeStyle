using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using D2L.CodeStyle.SpecTests;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers {

	[TestFixtureSource( nameof( GetSpecTests ) )]
	internal sealed class Spec : SpecTestFixtureBase {

		/// <summary>
		/// Parameterized test fixture of <see cref="SpecTest"/>.
		/// </summary>
		/// <param name="test">Tests returned by <see cref="GetSpecTests"/>.</param>
		public Spec( SpecTest test ) : base( test ) { }

		private static IEnumerable<SpecTest> GetSpecTests() {

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
			builder.AddAssemblyOf( typeof( ImmutableArray<> ) );

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach( var assembly in assemblies ) {
				if(!string.IsNullOrEmpty( assembly.Location ) ) {
					builder.Add( MetadataReference.CreateFromFile( assembly.Location ) );
				}
			}

			return builder.ToImmutable();
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

				string text = testAssembly.ReadEmbeddedResourceAsString( resourcePath );

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

				string source = assembly.ReadEmbeddedResourceAsString( specFilePath );

				yield return (specName, source);
			}
		}
	}
}
