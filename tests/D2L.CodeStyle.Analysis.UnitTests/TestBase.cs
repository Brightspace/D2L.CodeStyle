using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace D2L.CodeStyle.Analysis {

    public class TestBase {

        protected static CSharpCompilation Compile( string source ) {
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

        protected ITypeSymbol Type( string text ) {
            var source = $"namespace D2L {{ {text} }}";
            var compilation = Compile( source );

            var toReturn = compilation.GetSymbolsWithName(
                predicate: n => true,
                filter: SymbolFilter.Type
            ).OfType<ITypeSymbol>().FirstOrDefault();
            Assert.IsNotNull( toReturn );
            Assert.AreNotEqual( TypeKind.Error, toReturn.TypeKind );
            return toReturn;
        }

        protected IFieldSymbol Field( string text ) {
            var type = Type( "sealed class Fake { " + text + "; }" );

            var toReturn = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault();
            Assert.IsNotNull( toReturn );
            Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );
            return toReturn;
        }

        protected IPropertySymbol Property( string text ) {
            var type = Type( "sealed class Fake { " + text + "; }" );

            var toReturn = type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault();
            Assert.IsNotNull( toReturn );
            Assert.AreNotEqual( TypeKind.Error, toReturn.Type.TypeKind );
            return toReturn;
        }
    }

}
