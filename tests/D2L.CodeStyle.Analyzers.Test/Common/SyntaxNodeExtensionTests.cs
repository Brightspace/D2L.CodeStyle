using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System.Linq;
using static D2L.CodeStyle.Analyzers.Common.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Common {

    [TestFixture]
    internal sealed class SyntaxNodeExtensionTests {

        [Test]
        public void IsPropertyGetterImplemented_Yes_ReturnsTrue() {
            var prop = Property( "private string random { get { return \"\"; } }" );
            var syntax = prop.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax;
            Assert.IsNotNull( syntax );

            var isGetterImplemented = SyntaxNodeExtension.IsPropertyGetterImplemented( syntax );

            Assert.IsTrue( isGetterImplemented );
        }

        [Test]
        public void IsPropertyGetterImplemented_No_ReturnsFalse() {
            var prop = Property( "private string random { get; }" );
            var syntax = prop.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax;
            Assert.IsNotNull( syntax );

            var isGetterImplemented = SyntaxNodeExtension.IsPropertyGetterImplemented( syntax );

            Assert.IsFalse( isGetterImplemented );
        }
    }
}
