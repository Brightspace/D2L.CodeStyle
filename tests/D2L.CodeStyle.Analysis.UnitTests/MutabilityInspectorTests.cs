using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace D2L.CodeStyle.Analysis {
	[TestFixture]
	public class MutabilityInspectorTests {

		private readonly MutabilityInspector m_inspector = new MutabilityInspector();

		private static CSharpCompilation Compile( string source ) {
			var tree = CSharpSyntaxTree.ParseText( source );
			var compilation = CSharpCompilation.Create(
				assemblyName: "TestAssembly",
				syntaxTrees: new[] { tree },
				references: new[] { MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ) }
			);
			return compilation;
		}

		private ITypeSymbol Type( string text ) {
			var source = $"namespace D2L {{ {text} }}";
			var compilation = Compile( source );

			var toReturn = compilation.GetSymbolsWithName( 
				predicate: n => true, 
				filter: SymbolFilter.Type 
			).OfType<ITypeSymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			return toReturn;
		}

		private IFieldSymbol Field( string text ) {
			var type = Type( "class Fake { " + text + "; }" );

			var toReturn = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			return toReturn;
		}

		private IPropertySymbol Property( string text ) {
			var type = Type( "class Fake { " + text + "; }" );

			var toReturn = type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			return toReturn;
		}

		[Test]
		public void IsFieldMutable_Private_False() {
			var field = Field( "private int[] random" );

			Assert.IsFalse( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsFieldMutable_Readonly_False() {
			var field = Field( "readonly int[] random" );

			Assert.IsFalse( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsFieldMutable_PrivateAndReadonly_False() {
			var field = Field( "private readonly int[] random" );

			Assert.IsFalse( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsFieldMutable_PublicReadonly_False() {
			var field = Field( "public readonly int[] random" );

			Assert.IsFalse( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsFieldMutable_Public_True() {
			var field = Field( "public int[] random" );

			Assert.IsTrue( m_inspector.IsFieldMutable( field ) );
		}

		[Test]
		public void IsPropertyMutable_Private_False() {
			var prop = Property( "private int random { get; set; }" );

			Assert.IsFalse( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsPropertyMutable_Readonly_False() {
			var prop = Property( "int random { get; }" );

			Assert.IsFalse( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsPropertyMutable_PrivateSetter_False() {
			var prop = Property( "int random { get; private set; }" );

			Assert.IsFalse( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsPropertyMutable_PublicWithSetter_True() {
			var prop = Property( "public int random { get; set; }" );

			Assert.IsTrue( m_inspector.IsPropertyMutable( prop ) );
		}

		[Test]
		public void IsTypeMutable_ValueType_False() {
			var type = Type( "struct random { string hello; }" );

			Assert.IsFalse( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_ArrayType_True() {
			var type = Field( "int[] random" ).Type;

			Assert.IsTrue( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_KnownImmutableType_False() {
			var type = Field( "string random" ).Type;

			Assert.IsFalse( m_inspector.IsTypeMutable( type ) );
		}

		[Test]
		public void IsTypeMutable_LooksAtFieldsInType() {
			var field = Field( "public string random" );
			Assert.IsTrue( m_inspector.IsFieldMutable( field ) );

			Assert.IsTrue( m_inspector.IsTypeMutable( field.ContainingType ) );
		}

		[Test]
		public void IsTypeMutable_LooksAtPropertiesInType() {
			var prop = Property( "public string random { get; set; }" );
			Assert.IsTrue( m_inspector.IsPropertyMutable( prop ) );

			Assert.IsTrue( m_inspector.IsTypeMutable( prop.ContainingType ) );
		}

	}
}
