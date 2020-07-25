// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Serialization.ReflectionSerializerAnalyzer

using System;
using D2L.LP.Serialization;

namespace D2L.LP.Serialization {
	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
	public sealed class ReflectionSerializerAttribute : Attribute { }
	public static class ReflectionSerializer {
		[AttributeUsage( AttributeTargets.Constructor )]
		public sealed class ConstructorAttribute : Attribute { }
	}
}

namespace SpecTests {

	namespace Classes {

		namespace ConstructorStyle {

			namespace SinglePublic {

				[ReflectionSerializer]
				public sealed class SingleValue {
					// ↓↓↓
					public SingleValue( int value ) {
						Value = value;
					}
					public int Value { get; }
				}

				[ReflectionSerializer]
				public sealed class MultipleValues {
					// ↓↓↓
					public MultipleValues( int value0, string value1 ) {
						Value0 = value0;
						Value1 = value1;
					}
					public int Value0 { get; }
					public string Value1 { get; }
				}

				[ReflectionSerializer]
				public sealed class WithStaticConstructor {
					public WithStaticConstructor( int value ) {
						Value = value;
					}
					public int Value { get; }
					// ↓↓↓
					static WithStaticConstructor() { }
				}

				[ReflectionSerializer]
				public sealed class WithImplicittInternalConstructor {
					public WithImplicittInternalConstructor( int value ) {
						Value = value;
					}
					public int Value { get; }
					// ↓↓↓
					WithImplicittInternalConstructor() { }
				}

				[ReflectionSerializer]
				public sealed class WithExplicitInternalConstructor {
					public WithExplicitInternalConstructor( int value ) {
						Value = value;
					}
					public int Value { get; }
					// ↓↓↓
					internal WithExplicitInternalConstructor() { }
				}

				[ReflectionSerializer]
				public sealed class WithProtectedConstructor {
					public WithProtectedConstructor( int value ) {
						Value = value;
					}
					public int Value { get; }
					// ↓↓↓
					protected WithProtectedConstructor() { }
				}

				[ReflectionSerializer]
				public sealed class WithPrivateConstructor {
					public WithPrivateConstructor( int value ) {
						Value = value;
					}
					public int Value { get; }
					// ↓↓↓
					private WithPrivateConstructor() { }
				}
			}
		}

		namespace PropertyStyle {

			namespace DefaultConstructor {

				[ReflectionSerializer]
				public sealed class ImplicitConstructor {
					public int Value { get; set; }
				}

				[ReflectionSerializer]
				public sealed class ImplicitConstructor_WithStaticConstructor {
					public int Value { get; set; }
					// ↓↓↓
					static ImplicitConstructor_WithStaticConstructor() { }
				}
			}

			namespace EmptyConstructor {

				[ReflectionSerializer]
				public sealed class Explicit {
					// ↓↓↓
					public Explicit() { }
					public int Value { get; set; }
				}

				[ReflectionSerializer]
				public sealed class WithStaticConstructor {
					public WithStaticConstructor() { }
					public int Value { get; set; }
					// ↓↓↓
					static WithStaticConstructor() { }
				}

				[ReflectionSerializer]
				public sealed class WithImplicitInternalConstructor {
					public WithImplicitInternalConstructor() { }
					public int Value { get; set; }
					// ↓↓↓
					WithImplicitInternalConstructor( int value ) { }
				}

				[ReflectionSerializer]
				public sealed class WithInternalConstructor {
					public WithInternalConstructor() { }
					public int Value { get; set; }
					// ↓↓↓
					internal WithInternalConstructor( int value ) { }
				}

				[ReflectionSerializer]
				public sealed class WithProtectedConstructor {
					public WithProtectedConstructor() { }
					public int Value { get; set; }
					// ↓↓↓
					protected WithProtectedConstructor( int value ) { }
				}

				[ReflectionSerializer]
				public sealed class WithPrivateConstructor {
					public WithPrivateConstructor() { }
					public int Value { get; set; }
					// ↓↓↓
					private WithPrivateConstructor( int value ) { }
				}
			}
		}

		namespace MultiplePublicConstructors {

			/* ReflectionSerializer_MultiplePublicConstructors */
			[ReflectionSerializer]
			public sealed class EmptyAndNonEmpty {
				public EmptyAndNonEmpty() { }
				public EmptyAndNonEmpty( int arg0 ) { }
			} /**/

			/* ReflectionSerializer_MultiplePublicConstructors */
			[ReflectionSerializer]
			public sealed class ManyNonEmpty {
				public ManyNonEmpty( int arg0 ) { }
				public ManyNonEmpty( int arg0, long arg1 ) { }
			} /**/
		}

		namespace NoPublicConstructors {

			public abstract class AbstractClass {
				protected AbstractClass() { }
			}

			/* ReflectionSerializer_NoPublicConstructors */
			[ReflectionSerializer]
			public sealed class Private {
				private Private() { }
			} /**/

			/* ReflectionSerializer_NoPublicConstructors */
			[ReflectionSerializer]
			public sealed class Protected {
				internal Protected() { }
			} /**/

			/* ReflectionSerializer_NoPublicConstructors */
			[ReflectionSerializer]
			public sealed class ImplicittInternal {
				internal ImplicittInternal() { }
			} /**/

			/* ReflectionSerializer_NoPublicConstructors */
			[ReflectionSerializer]
			public sealed class ExplicitInternal {
				internal ExplicitInternal() { }
			} /**/
		}
	}

	namespace Structs {

		public struct SingleConstructor {
			public SingleConstructor( int value ) {
				Value = value;
			}
			public int Value { get; }
		}

		/* ReflectionSerializer_MultiplePublicConstructors */
		[ReflectionSerializer]
		public struct MultipleConstructors {
			public MultipleConstructors( int value0 ) {
				Value0 = value0;
				Value1 = string.Empty;
			}
			public MultipleConstructors( int value0, string value1 ) {
				Value0 = value0;
				Value1 = value1;
			}
			public int Value0 { get; }
			public string Value1 { get; }
		} /**/
	}
}

namespace SmokeTests {

	public sealed class RandomWithMultipleConstructors {
		public MultipleConstructors() { }
		public MultipleConstructors( int arg0 ) { }
	}

	[ReflectionSerializer()]
	public sealed class AttributeWithParentheses { }

	[ReflectionSerializer( "junk" )]
	public sealed class AttributeWithBonusArgs { }

	[System.ComponentModel.Category( "Not related" )]
	public sealed class RandomAttribute { }

	[]
	public sealed class JustSomeBrackets { }
}
