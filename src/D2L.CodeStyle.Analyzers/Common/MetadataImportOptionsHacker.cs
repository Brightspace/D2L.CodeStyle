using System;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common {
	internal static class MetadataImportOptionsHacker {
		private static readonly Lazy<object> s_metadataImportOptionsAll = new Lazy<object>(
			() => Enum.Parse(
				Type.GetType( "Microsoft.CodeAnalysis.MetadataImportOptions, Microsoft.CodeAnalysis" ),
				"All"
			),
			LazyThreadSafetyMode.ExecutionAndPublication
		);

		/// <summary>
		/// This method modifies the compilation using reflection to import all 
		/// metadata. We do this because analyzers can behave differently depenending
		/// on what context they are running in (see 
		/// https://github.com/dotnet/roslyn/issues/20373). 
		/// </summary>
		/// <remarks>
		/// If the issue linked to above is resolved correctly, then this hack
		/// may no longer be necessary, and we can feel free to delete it.
		/// </remarks>
		internal static void HackImportAllMetadata( this Compilation compilation) {
			var options = compilation.Options;

			var metadataImportOptionsProperty = options.GetType().GetProperty(
				"MetadataImportOptions_internal_protected_set",
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
			);

			if( metadataImportOptionsProperty == null ) {
				throw new InvalidOperationException( "Unable to obtain the 'MetadataImportOptions_internal_protected_set' " +
					"property using reflection. Mostly likely Microsoft changed something. Maybe check " +
					"https://github.com/dotnet/roslyn/issues/20373 to see if it's been resolved :)" );
			}

			metadataImportOptionsProperty.SetValue( options, s_metadataImportOptionsAll.Value );
		}

		/// <summary>
		/// Throws if the compilation is not configured to import all metadata.
		/// See <see cref="HackImportAllMetadata"/> for additional information.
		/// </summary>
		/// <param name="compilation"></param>
		internal static void ThrowIfNotImportingAllMetadata( this Compilation compilation ) {
			var options = compilation.Options;

			var metadataImportOptionsProperty = options.GetType().GetProperty(
				"MetadataImportOptions",
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
			);

			if( metadataImportOptionsProperty == null ) {
				throw new InvalidOperationException( "Unable to obtain the 'MetadataImportOptions_internal_protected_set' " +
					"property using reflection. Mostly likely Microsoft changed something. Maybe check " +
					"https://github.com/dotnet/roslyn/issues/20373 to see if it's been resolved :)" );
			}

			var value = metadataImportOptionsProperty.GetValue( options );

			if( !value.Equals( s_metadataImportOptionsAll.Value ) ) {
				throw new NotSupportedException( $"Expected MetadataImportOptions value of {s_metadataImportOptionsAll.Value}, but found {value}." );
			}
		}
	}
}
