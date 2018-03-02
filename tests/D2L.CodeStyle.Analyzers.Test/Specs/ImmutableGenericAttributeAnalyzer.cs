// analyzer: D2L.CodeStyle.Analyzers.Immutability.ImmutableGenericAttributeAnalyzer

using System;
using SpecTests;
using D2L.CodeStyle.Annotations;

// valid cases
[assembly: Objects.ImmutableGeneric( typeof( IGeneric<string> ) )]
[assembly: Objects.ImmutableGeneric( typeof( IGeneric<Foo> ) )]
[assembly: Objects.ImmutableGeneric( typeof( IComparable<Foo> ) )]

// invalid cases
[assembly: /* ImmutableGenericAttributeAppliedToNonGenericType(System.String) */ Objects.ImmutableGeneric( typeof( string ) ) /**/]
[assembly: /* ImmutableGenericAttributeAppliedToNonGenericType(SpecTests.Foo) */ Objects.ImmutableGeneric( typeof( Foo ) ) /**/]
[assembly: /* ImmutableGenericAttributeAppliedToOpenGenericType(SpecTests.IGeneric<>) */ Objects.ImmutableGeneric( typeof( IGeneric<> ) ) /**/]
[assembly: /* ImmutableGenericAttributeInWrongAssembly(System.IComparable<System.String>) */ Objects.ImmutableGeneric( typeof( IComparable<string> ) ) /**/]

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		[AttributeUsage( validOn: AttributeTargets.Assembly )]
		public sealed class ImmutableGenericAttribute : Attribute {
			public ImmutableGenericAttribute( Type type ) { }
		}
	}
}

namespace SpecTests {
	internal class Foo { }
	internal interface IGeneric<T> { }
}

