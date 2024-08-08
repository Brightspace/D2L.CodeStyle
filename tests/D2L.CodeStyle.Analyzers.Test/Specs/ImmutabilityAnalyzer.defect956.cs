// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer, D2L.CodeStyle.Analyzers

// Issue #956

using static D2L.CodeStyle.Annotations.Objects;

namespace Defect956;

[ConditionallyImmutable]
interface IFoo<[ConditionallyImmutable.OnlyIf]A> {}

[ConditionallyImmutable]
record Foo<[ConditionallyImmutable.OnlyIf]A, [ConditionallyImmutable.OnlyIf]B>(
  A A
  B B
) : IFoo<A>;

static class Demo {
  public static readonly Foo<int, int> ThisIsFine = new( 1, 2 );

  public static readonly
    /* NonImmutableTypeHeldByImmutable(class, Foo<int<Dictionary<int, int>>, ) */ Foo<int, Dictionary<int, int>> /**/
    ObviouslyBad = new( 3, new() );

  // That this is allowed is a problem: we can cast back to Foo<int, Dictionary<int, int>>
  // and poke at the dictionary, or maybe the dictionary got smuggled in from elsehwere
  // where its still accessible etc.
  // The point is that IFoo<A> looks immutable if A does, and Foo<A, B> was able to implement
  // it for any B
  public static readonly IFoo<int> Defect = (IFoo<int>)(new Foo<int, Dictionary<int, int>>( 4, new() ));
}
