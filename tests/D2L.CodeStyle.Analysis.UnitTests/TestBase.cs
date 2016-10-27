using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Collections.Immutable;
using System.Linq;

namespace D2L.CodeStyle.Analysis {

    public static class RoslynSymbolFactory {

        public static CSharpCompilation Compile( string source ) {
            var tree = CSharpSyntaxTree.ParseText( source );
            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { tree },
                references: new[] {
                    MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ),
                    MetadataReference.CreateFromFile( typeof( ImmutableArray ).Assembly.Location )
                }
            );
            return compilation;
        }

        public static ITypeSymbol Type( string text ) {
            var source = $"using System; namespace D2L {{ {text} }}";
            var compilation = Compile( source );

            var toReturn = compilation.GetSymbolsWithName(
                predicate: n => true,
                filter: SymbolFilter.Type
            ).OfType<ITypeSymbol>().FirstOrDefault();
            Assert.IsNotNull( toReturn );
            Assert.AreNotEqual( TypeKind.Error, toReturn.TypeKind );
            return toReturn;
        }

        public static IFieldSymbol Field( string text ) {
            var type = Type( "sealed class Fake { " + text + "; }" );

            var toReturn = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault();
            Assert.IsNotNull( toReturn );
            Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );
            return toReturn;
        }

        public static IPropertySymbol Property( string text ) {
            var type = Type( "sealed class Fake { " + text + "; }" );

            var toReturn = type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault();
            Assert.IsNotNull( toReturn );
            Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );
            return toReturn;
        }
    }

}
