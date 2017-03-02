using NUnit.Framework;
using System.IO;

namespace D2L.CodeStyle.Analyzers.Common {

	internal sealed class UtilsTests {

		private readonly Utils m_utils = new Utils();

		[Test]
		public void IsGeneratedCodefile_NotCSharpFile_False() {
			string file = Path.Combine( "proj", "random.js" );

			Assert.IsFalse( m_utils.IsGeneratedCodefile( file ) );
		}

		[Test]
		public void IsGeneratedCodefile_CSharpFileNotGenerated_False() {
			string file = Path.Combine( "proj", "random.cs" );

			Assert.IsFalse( m_utils.IsGeneratedCodefile( file ) );
		}

		[Test]
		public void IsGeneratedCodefile_CSharpFileInGeneratedFolder_True() {
			string file = Path.Combine( "proj", ".generated", "random.cs" );

			Assert.IsTrue( m_utils.IsGeneratedCodefile( file ) );
		}

		[Test]
		public void IsGeneratedCodefile_CSharpFileInFolderWithGeneratedName_False() {
			string file = Path.Combine( "proj.generated", "random.cs" );

			Assert.IsFalse( m_utils.IsGeneratedCodefile( file ) );
		}

		[Test]
		public void IsGeneratedCodeFile_ResourceFile_True() {
			string file = Path.Combine( "proj", "random.Designer.cs" );

			Assert.IsTrue( m_utils.IsGeneratedCodefile( file ) );
		}
	}
}
