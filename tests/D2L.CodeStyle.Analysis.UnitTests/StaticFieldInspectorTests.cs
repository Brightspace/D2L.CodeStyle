using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace D2L.CodeStyle.Analysis {
	[TestFixture]
	public class StaticFieldInspectorTests {
		private readonly CSharpCompilation m_compilation;
		private readonly List<FieldDeclarationSyntax> m_fields;
		private readonly SyntaxTree m_tree;
		private readonly SemanticModel m_model;

		public StaticFieldInspectorTests() {
			m_tree = CreateDummySyntaxTree();

			m_compilation = CreateCompilation()
				.AddSyntaxTrees( m_tree );

			m_model = m_compilation.GetSemanticModel( m_tree );

			m_fields = m_tree
				.GetRoot()
				.DescendantNodes()
				.OfType<FieldDeclarationSyntax>()
				.ToList();
		}

		[Test]
		public void IsMultiTenantSafe_ArrayOfInt_False() {
			var field = GetField( "m_staticReadonlyArrayOfInt" );

			BadStaticReason? actual;
			bool safe = StaticFieldInspector.IsMultiTenantSafe(
				m_model,
				field,
				out actual
			);

			Assert.IsFalse( safe );
			Assert.AreEqual( BadStaticReason.NonImmutable, actual );
		}

		private FieldDeclarationSyntax GetField( string name ) {
			return m_fields.Single( f => f.Declaration.Variables.First().Identifier.ValueText == name );
		}

		private static SyntaxTree CreateDummySyntaxTree() {
			return CSharpSyntaxTree.ParseText(
@"using System;

namespace D2L {
	class Fake {
		private static readonly int[] m_staticReadonlyArrayOfInt;
	}
}" );
		}

		private static CSharpCompilation CreateCompilation() {
			var dotnetRefLocation = typeof( object ).Assembly.Location;
			var dotnetRef = MetadataReference.CreateFromFile( dotnetRefLocation );

			return CSharpCompilation.Create( "FakeAssembly" )
				.AddReferences( dotnetRef );
		}
	}
}
