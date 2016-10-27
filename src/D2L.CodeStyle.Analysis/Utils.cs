using System.IO;

namespace D2L.CodeStyle.Analysis {

	public sealed class Utils {

		private static readonly string GeneratedFolderPathSegment = Path.DirectorySeparatorChar + ".generated" + Path.DirectorySeparatorChar;
		private static readonly string ResourceFileSuffix = ".Designer.cs";

		public bool IsGeneratedCodefile( string path ) {
			path = Path.GetFullPath( path );

			if( Path.GetExtension( path ) == ".cs") {
				if( path.Contains( GeneratedFolderPathSegment ) ) {
					return true;
				}
				if( path.EndsWith( ResourceFileSuffix ) ) {
					return true;
				}
			}


			return false;
		}
	}
}
