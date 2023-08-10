using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.Async.Generator.SyncGenerator;

namespace D2L.CodeStyle.Analyzers.Tests.Async.Generator;

[TestFixture]
internal class FileCollectorTests {
	[Test]
	public void Empty() {
		var root = CSharpSyntaxTree.ParseText( @"
namespace X;

public sealed class Y {
	void SomeMethod() {}
}" ).GetCompilationUnitRoot();

		var collector = FileCollector.Create(
			root,
			ImmutableArray<(TypeDeclarationSyntax, string)>.Empty 
		);

		Assert.AreEqual( @"#pragma warning disable CS1572
", collector.CollectSource() );
	}

	[Test]
	public void Basic() {
		var root = CSharpSyntaxTree.ParseText( @"
using Foo;

namespace X;

public sealed class Y {
	internal class Z {}

	void MyMethodBefore() {
		Console.WriteLine( ""Hello"" );
	}
}" ).GetCompilationUnitRoot();

		SyntaxNode myMethodBefore = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

		var collector = FileCollector.Create(
			root,
			ImmutableArray.Create(
				((TypeDeclarationSyntax)myMethodBefore.Parent, "\tany text\r\n")
			)
		);

		Assert.AreEqual( @"#pragma warning disable CS1572

using Foo;

namespace X;

partial class Y {
	any text
}",
			collector.CollectSource()
		);
	}

	[Test]
	public void Generics() {
		var root = CSharpSyntaxTree.ParseText( @"
using Foo;

namespace X;

public sealed class Y<T, U> where T : new where U : T {
	internal class Z {}

	void MyMethodBefore() {
		Console.WriteLine( ""Hello"" );
	}
}" ).GetCompilationUnitRoot();

		SyntaxNode myMethodBefore = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

		var collector = FileCollector.Create(
			root,
			ImmutableArray.Create(
				((TypeDeclarationSyntax)myMethodBefore.Parent, "\tany text\r\n")
			)
		);

		Assert.AreEqual( @"#pragma warning disable CS1572

using Foo;

namespace X;

partial class Y<T, U> {
	any text
}",
			collector.CollectSource()
		);
	}
	[TestCase( "class" )] // static/selaed come before partial and don't need to show up in the other partials
	[TestCase( "struct" )] // ditto for readonly
	[TestCase( "record" )]
	[TestCase( "interface" )]
	[TestCase( "record class" )]
	[TestCase( "record struct" )]
	public void AllTypes( string kind ) {
		var root = CSharpSyntaxTree.ParseText( @$"
public partial {kind} X {{
	void MyMethodBefore() {{
		Console.WriteLine( ""Hello"" );
	}}
}}" ).GetCompilationUnitRoot();

		SyntaxNode myMethodBefore = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

		var collector = FileCollector.Create(
			root,
			ImmutableArray.Create(
				((TypeDeclarationSyntax)myMethodBefore.Parent, "\tany text\r\n")
			)
		);

		Assert.AreEqual( @$"#pragma warning disable CS1572

partial {kind} X {{
	any text
}}",
			collector.CollectSource()
		);
	}

	[Test]
	public void Nesting() {
		var root = CSharpSyntaxTree.ParseText( @"
using Foo;

namespace A.B.C {
	namespace X {
		public sealed partial class Y {
			struct Ignored1 {}

			internal partial class Z {
				void MyMethodBefore() {
					Console.WriteLine( ""Hello"" );
				}
			}

			class Ignored2 {}
		}
	}
}" ).GetCompilationUnitRoot();

		SyntaxNode myMethodBefore = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

		var collector = FileCollector.Create(
			root,
			ImmutableArray.Create(
				((TypeDeclarationSyntax)myMethodBefore.Parent, "\t\t\t\tany text\r\n")
			)
		);

		Assert.AreEqual( @"#pragma warning disable CS1572

using Foo;

namespace A.B.C {
	namespace X {
		partial class Y {

			partial class Z {
				any text
			}
		}
	}
}",
			collector.CollectSource()
		);
	}

	[Test]
	public void Complicated() {
		// A mix of nesting and unused things with multiple methods

		var root = CSharpSyntaxTree.ParseText( @"
using Foo;

namespace A.B.C {
	using Bar;

	namespace X {
		using Quux;
		using System;

		interface Ignored {}

		public sealed partial class Y {
			struct Ignored1 {}

			internal partial class Z {
				void MyMethodBefore() {
					Console.WriteLine( ""Hello"" );
				}

				void MyMethodBefore2() {
					Console.WriteLine( ""Bye"" );
				}
			}

			class Ignored2 {}

			private static partial class W {
				void MyBeforeMethod3() {}
			}
		}
	}
}

namespace Ignored {
	namespace Ignored {
		class Ignored {}
	}
}

namespace Q {
	public partial record struct ZZZ {
		public static partial class WWW {
			private partial interface IWhat {
				void MyBeforeMethod4() { }
			}
		}
	}
}
" ).GetCompilationUnitRoot();

		var myMethodsBefore = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
			.Select( ( node, idx ) => ((TypeDeclarationSyntax)node.Parent, $"\t\t\t\tany text{idx}\r\n") );

		var collector = FileCollector.Create(
			root,
			myMethodsBefore.ToImmutableArray()
		);

		Assert.AreEqual( @"#pragma warning disable CS1572

using Foo;

namespace A.B.C {
	using Bar;

	namespace X {
		using Quux;
		using System;

		partial class Y {

			partial class Z {
				any text0
				any text1
			}

			partial class W {
				any text2
			}
		}
	}
}

namespace Q {
	partial record struct ZZZ {
		partial class WWW {
			partial interface IWhat {
				any text3
			}
		}
	}
}
",
			collector.CollectSource()
		);
	}
}
