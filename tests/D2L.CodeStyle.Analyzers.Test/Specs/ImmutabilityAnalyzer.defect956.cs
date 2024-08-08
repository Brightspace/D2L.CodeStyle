// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzer, D2L.CodeStyle.Analyzers

// Issue #956

using static D2L.CodeStyle.Annotations.Objects;

namespace Defect956;

[ConditionallyImmutable]
interface IFoo<[ConditionallyImmutable.OnlyIf]A> {}


[ConditionallyImmutable]
class Foo<[ConditionallyImmutable.OnlyIf]A, [ConditionallyImmutable.OnlyIf]B> : IFoo<A> {
  public readonly A m_a;
  public readonly B m_b;

  public Foo( A a, B b ) {
    m_a = a;
    m_b = b;
  }

  public static IFoo<int> Evil( int x, Dictionary<int, int> mutability ) {
    // Here we are constructing a Foo; that ObjectCreationExpression would
    // return something that is correctly judged to be mutable/not-immutable.
    var foo = new Foo( x, mutability );

    // This assignment is allowed by the language, but our analyzer doesn't
    // realize we are losing that mutability judgement.
    IFoo<int> evilFoo = foo;

    // We are returning this and callers will think it is Immutable based on
    // IFoo<int>, but they can mutate it through a backdoor
    return evilFoo;
  }
}