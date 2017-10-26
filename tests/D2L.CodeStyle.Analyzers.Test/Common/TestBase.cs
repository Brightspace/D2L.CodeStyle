using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Test.Verifiers;

namespace D2L.CodeStyle.Analyzers.Common {

	internal sealed class TestSymbol<T> where T : ISymbol {
		internal readonly T Symbol;
		internal readonly SemanticModel SemanticModel;
		public TestSymbol( T symbol, SemanticModel semanticModel ) {
			Symbol = symbol;
			SemanticModel = semanticModel;
		}
	}

	internal static class RoslynSymbolFactory {

		internal static string TestAssemblyName = DiagnosticVerifier.TestProjectName;
		internal static string RootNamespace = "D2L";
		internal static string RootClass = "Fake";

		internal static CSharpCompilation Compile( string source ) {
			var tree = CSharpSyntaxTree.ParseText( source );
			var compilation = CSharpCompilation.Create(
				assemblyName: TestAssemblyName,
				syntaxTrees: new[] { tree },
				references: new[] {
					MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ),
					MetadataReference.CreateFromFile( typeof( ImmutableArray ).Assembly.Location )
				}
			);
			return compilation;
		}

		internal static TestSymbol<ITypeSymbol> Type( string text ) {
			var source = $"using System; namespace {RootNamespace} {{ {text} }}";
			var compilation = Compile( source );

			var toReturn = compilation.GetSymbolsWithName(
				predicate: n => true,
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			Assert.AreNotEqual( TypeKind.Error, toReturn.TypeKind );

			return new TestSymbol<ITypeSymbol>( toReturn, compilation.GetSemanticModel( toReturn.DeclaringSyntaxReferences.First().SyntaxTree ) );
		}

		internal static TestSymbol<IFieldSymbol> Field( string text ) {
			var type = Type( "sealed class " + RootClass + " { " + text + "; }" );

			var toReturn = type.Symbol.GetMembers().OfType<IFieldSymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );

			return new TestSymbol<IFieldSymbol>( toReturn, type.SemanticModel );
		}

		internal static TestSymbol<IPropertySymbol> Property( string text ) {
			var type = Type( "sealed class " + RootClass + " { " + text + "; }" );

			var toReturn = type.Symbol.GetMembers().OfType<IPropertySymbol>().FirstOrDefault();
			Assert.IsNotNull( toReturn );
			Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );

			return new TestSymbol<IPropertySymbol>( toReturn, type.SemanticModel );
		}
	}

}
