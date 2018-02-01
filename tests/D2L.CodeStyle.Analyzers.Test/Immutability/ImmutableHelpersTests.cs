using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[TestFixture]
	internal sealed class ImmutableHelpersTests {

		private const string AnnotationsPreamble = @"
using System;
using static D2L.CodeStyle.Annotations.Objects;
 
namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public sealed class Immutable : Attribute {
 
			public Except Except { get; set; }
 
		}
 
		[Flags]
		public enum Except {
 
			None = 0,
			ItHasntBeenLookedAt = 1,
			ItsSketchy = 2,
			ItsStickyDataOhNooo = 4,
			WeNeedToMakeTheAnalyzerConsiderThisSafe = 8,
			ItsUgly = 16,
			ItsOnDeathRow = 32
 
		}
	}
}
";

		[Test]
		public void TryGetDirectImmutableExceptions_WhenTypeDoesNotHaveImmutableAttribute_ReturnsFalse() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"public class Foo { }" );

			ImmutableHashSet<string> exceptions;
			bool result = ty.Symbol.TryGetDirectImmutableExceptions( out exceptions );

			Assert.That( result, Is.False );
		}

		[Test]
		public void TryGetDirectImmutableExceptions_WhenNoSpecifiedExceptions_ReturnsDefaultReasons() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable]
public class Foo { }
" );

			ImmutableHashSet<string> immutabilityExceptions;
			bool result = ty.Symbol.TryGetDirectImmutableExceptions( out immutabilityExceptions );

			Assert.That( result, Is.True );
			Assert.That( immutabilityExceptions, Is.EquivalentTo( ImmutableHelpers.DefaultImmutabilityExceptions ) );
		}

		[Test]
		public void TryGetDirectImmutableExceptions_WhenSpecifiedExceptions_ReturnsSpecifiedExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItsUgly | Except.WeNeedToMakeTheAnalyzerConsiderThisSafe | Except.ItsSketchy )]
public class Foo { }
" );

			ImmutableHashSet<string> immutabilityExceptions;
			bool result = ty.Symbol.TryGetDirectImmutableExceptions( out immutabilityExceptions );

			Assert.That( result, Is.True );
			Assert.That( immutabilityExceptions, Is.EquivalentTo( new[] {
				"WeNeedToMakeTheAnalyzerConsiderThisSafe",
				"ItsUgly",
				"ItsSketchy"
			} ) );
		}

		[Test]
		public void TryGetDirectImmutableExceptions_WhenSpecifiedNoneReasons_ReturnsEmptySet() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.None )]
public class Foo { }
" );

			ImmutableHashSet<string> immutabilityExceptions;
			bool result = ty.Symbol.TryGetDirectImmutableExceptions( out immutabilityExceptions );

			Assert.That( result, Is.True );
			Assert.That( immutabilityExceptions, Is.Empty );
		}

		[Test]
		public void GetInheritedImmutableExceptions_WhenNoInheritedTypes_ReturnsEmptyDict() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
public class Foo { }
" );

			ImmutableDictionary<ISymbol, ImmutableHashSet<string>> inheritedExceptions = ty.Symbol.GetInheritedImmutableExceptions();

			Assert.That( inheritedExceptions, Is.Empty );
		}

		[Test]
		public void GetInheritedImmutableExceptions_WhenNoImmutableInheritedTypes_ReturnsEmptyDict() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
public interface IFoo { }
public class FooBase { }

public sealed class Foo : FooBase, IFoo { }
" );

			ImmutableDictionary<ISymbol, ImmutableHashSet<string>> inheritedExceptions = ty.Symbol.GetInheritedImmutableExceptions();

			Assert.That( inheritedExceptions, Is.Empty );
		}

		[Test]
		public void GetInheritedImmutableExceptions_WhenImmutableInheritedTypes_ReturnsEachInheritedTypesExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItHasntBeenLookedAt )]
public interface IFoo { }
[Immutable( Except = Except.ItsUgly | Except.ItsSketchy )]
public class FooBase { }

public sealed class Foo : FooBase, IFoo { }
" );

			ImmutableDictionary<ISymbol, ImmutableHashSet<string>> inheritedExceptions = ty.Symbol.GetInheritedImmutableExceptions();

			Assert.That( inheritedExceptions, Has.Count.EqualTo( 2 ) );

			ISymbol ifooSymbol = inheritedExceptions.Keys.FirstOrDefault( s => s.Name == "IFoo" );
			Assert.That( ifooSymbol, Is.Not.Null );

			Assert.That( inheritedExceptions[ifooSymbol], Is.EquivalentTo( new[] { "ItHasntBeenLookedAt" } ) );

			ISymbol fooBaseSymbol = inheritedExceptions.Keys.FirstOrDefault( s => s.Name == "FooBase" );
			Assert.That( fooBaseSymbol, Is.Not.Null );

			Assert.That( inheritedExceptions[fooBaseSymbol], Is.EquivalentTo( new[] { "ItsUgly", "ItsSketchy" } ) );
		}

		[Test]
		public void GetInheritedImmutableExceptions_WhenBaseTypeIndirectlyImmutable_ReturnsInheritedTypesExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItHasntBeenLookedAt )]
public interface IFoo { }
public class FooBase : IFoo { }

public sealed class Foo : FooBase { }
" );

			ImmutableDictionary<ISymbol, ImmutableHashSet<string>> inheritedExceptions = ty.Symbol.GetInheritedImmutableExceptions();

			Assert.That( inheritedExceptions, Has.Count.EqualTo( 1 ) );

			ISymbol ifooSymbol = inheritedExceptions.Keys.FirstOrDefault( s => s.Name == "IFoo" );
			Assert.That( ifooSymbol, Is.Not.Null );

			Assert.That( inheritedExceptions[ifooSymbol], Is.EquivalentTo( new[] { "ItHasntBeenLookedAt" } ) );
		}

		[Test]
		public void GetInheritedImmutableExceptions_WhenInterfaceIndirectlyImmutable_ReturnsInheritedTypesExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItHasntBeenLookedAt )]
public interface IFoo { }
public interface IFoo2 : IFoo { }

public sealed class Foo : IFoo2 { }
" );

			ImmutableDictionary<ISymbol, ImmutableHashSet<string>> inheritedExceptions = ty.Symbol.GetInheritedImmutableExceptions();

			Assert.That( inheritedExceptions, Has.Count.EqualTo( 1 ) );

			ISymbol ifooSymbol = inheritedExceptions.Keys.FirstOrDefault( s => s.Name == "IFoo" );
			Assert.That( ifooSymbol, Is.Not.Null );

			Assert.That( inheritedExceptions[ifooSymbol], Is.EquivalentTo( new[] { "ItHasntBeenLookedAt" } ) );
		}

		[Test]
		public void GetAllImmutableExceptions_WhenNotImmutableAndNoInheritedTypes_ThrowsException() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
public sealed class Foo { }
" );

			Assert.That( () => ty.Symbol.GetAllImmutableExceptions(), Throws.Exception );
		}

		[Test]
		public void GetAllImmutableExceptions_WhenNotImmutableAndNoImmutableInheritedTypes_ThrowsException() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
public interface IFoo { }
public class FooBase { }

public sealed class Foo : FooBase, IFoo { }
" );

			Assert.That( () => ty.Symbol.GetAllImmutableExceptions(), Throws.Exception );
		}

		[Test]
		public void GetAllImmutableExceptions_WhenImmutableAndNoImmutableInheritedTypes_ReturnsDefinedExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
public interface IFoo { }
public class FooBase { }

[Immutable( Except = Except.ItsUgly | Except.ItsSketchy )]
public sealed class Foo : FooBase, IFoo { }
" );
			ImmutableHashSet<string> allExceptions = ty.Symbol.GetAllImmutableExceptions();
			Assert.That( allExceptions, Is.EquivalentTo( new[] { "ItsUgly", "ItsSketchy" } ) );
		}

		[Test]
		public void GetAllImmutableExceptions_WhenNotMarkedImmutableAndImmutableInheritedTypes_ReturnsIntersectionOfInheritedExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItsUgly | Except.ItHasntBeenLookedAt )]
public interface IFoo { }
[Immutable( Except = Except.ItsUgly | Except.ItsSketchy )]
public class FooBase { }

public sealed class Foo : FooBase, IFoo { }
" );
			ImmutableHashSet<string> allExceptions = ty.Symbol.GetAllImmutableExceptions();
			Assert.That( allExceptions, Is.EquivalentTo( new[] { "ItsUgly" } ) );
		}

		[Test]
		public void GetAllImmutableExceptions_WhenMarkedImmutableAndImmutableInheritedTypes_ReturnsDirectlyDefinedExceptions() {

			TestSymbol<ITypeSymbol> ty = CompileAndGetFooType( @"
[Immutable( Except = Except.ItsUgly )]
public interface IFoo { }
[Immutable( Except = Except.ItsUgly )]
public class FooBase { }

[Immutable( Except = Except.ItsSketchy )]
public sealed class Foo : FooBase, IFoo { }
" );
			ImmutableHashSet<string> allExceptions = ty.Symbol.GetAllImmutableExceptions();
			Assert.That( allExceptions, Is.EquivalentTo( new[] { "ItsSketchy" } ) );
		}

		private static TestSymbol<ITypeSymbol> CompileAndGetFooType( string source ) {
			source = $"namespace D2L {{ {source} }}";
			source = AnnotationsPreamble + source;

			var compilation = Compile( source );
			var symbol = compilation.GetSymbolsWithName(
				predicate: n => n == "Foo",
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();

			Assert.IsNotNull( symbol );
			Assert.AreNotEqual( TypeKind.Error, symbol.TypeKind );

			return new TestSymbol<ITypeSymbol>( symbol, compilation );
		}

	}
}
