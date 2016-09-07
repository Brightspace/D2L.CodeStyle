using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace D2L.CodeStyle.Analysis {

    public sealed class Utils {

        private static readonly string GeneratedFolderPathSegment = Path.DirectorySeparatorChar + ".generated" + Path.DirectorySeparatorChar;

        public bool IsGeneratedCodefile( string path ) {
            path = Path.GetFullPath( path );

            if( Path.GetExtension( path ) == ".cs") {
                if( path.Contains( GeneratedFolderPathSegment ) ) {
                    return true;
                }
            }


            return false;
        }
    }
}
