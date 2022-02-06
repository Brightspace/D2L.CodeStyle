namespace D2L.CodeStyle.SpecTests.Generators {

	internal static class ProjectPathUtility {

		public static string GetProjectRelativePath(
				string projectDirectory,
				string includePath
			) {

			// santizied
			projectDirectory = Path.GetFullPath( projectDirectory );
			includePath = Path.GetFullPath( includePath );

			if( !includePath.StartsWith( projectDirectory, StringComparison.Ordinal ) ) {
				throw new ArgumentException( message: "Include path must be a child of the project directory" );
			}

			string projectRelativePath = includePath
				.Substring( projectDirectory.Length )
				.Trim( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );

			return projectRelativePath;
		}
	}
}
