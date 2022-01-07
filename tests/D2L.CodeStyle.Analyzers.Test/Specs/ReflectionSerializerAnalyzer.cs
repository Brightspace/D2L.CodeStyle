// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Serialization.ReflectionSerializerAnalyzer

using System;
using D2L.LP.Serialization;

namespace D2L.LP.Serialization {

	[AttributeUsage(
		AttributeTargets.Class | AttributeTargets.Struct,
		AllowMultiple = false,
		Inherited = false
	)]
	public sealed class ReflectionSerializerAttribute : Attribute { }

	public static partial class ReflectionSerializer {

		[AttributeUsage(
			validOn: AttributeTargets.Property,
			AllowMultiple = false,
			Inherited = false
		)]
		public sealed class IgnoreAttribute : Attribute { }
	}
}

namespace SpecTests {

	namespace Records {

		[ReflectionSerializer]
		public sealed record PrimaryConstructorOnly( int X );

		[ReflectionSerializer]
		public sealed partial record PartialPrimaryConstructorOnly( int X );
		public sealed partial record PartialPrimaryConstructorOnly { }

		[ReflectionSerializer]
		public sealed record PublicConstructorOnly {
			public PublicConstructorOnly( int x ) {
				X = x;
			}
			public int X { get; }
		}

		[ReflectionSerializer]
		public sealed partial record PartialPublicConstructorOnly {
			public PartialPublicConstructorOnly( int x ) {
				X = x;
			}
			public int X { get; }
		}
		public sealed partial record PartialPublicConstructorOnly { }

		[ReflectionSerializer]
		public sealed record PrimaryAndPublicConstructor( int X, int Y ) {
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PrimaryAndPublicConstructor /**/( int x ) : this( X: x, Y: 0 ) { }
		}

		[ReflectionSerializer]
		public sealed record PrimaryAndMultiplePublicConstructors( int X, int Y, int Z ) {
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PrimaryAndMultiplePublicConstructors /**/( int x ) : this( X: x, Y: 0, Z: 0 ) { }
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PrimaryAndMultiplePublicConstructors /**/( int x, int y ) : this( X: x, Y: y, Z: 0 ) { }
		}

		[ReflectionSerializer]
		public sealed partial record /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PartialPrimaryAndPublicConstructorWithPrimaryAttributed /**/ ( int X, int Y );
		public sealed partial record PartialPrimaryAndPublicConstructorWithPrimaryAttributed {
			public PartialPrimaryAndPublicConstructorWithPrimaryAttributed( int x ) : this( X: x, Y: 0 ) { }
		}

		public sealed partial record PartialPrimaryAndPublicConstructorWithPublicAttributed( int X, int Y );
		[ReflectionSerializer]
		public sealed partial record /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PartialPrimaryAndPublicConstructorWithPublicAttributed /**/ {
			public PartialPrimaryAndPublicConstructorWithPublicAttributed( int x ) : this( X: x, Y: 0 ) { }
		}

		[ReflectionSerializer]
		public sealed record TwoPublicConstructor {
			public TwoPublicConstructor( int x, int y ) {
				X = x;
				Y = y;
			}
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ TwoPublicConstructor /**/( int X ) : this( x: X, y: 0 ) { }
			public int X { get; }
			public int Y { get; }
		}

		[ReflectionSerializer]
		public sealed partial record /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PartialTwoPublicConstructor /**/ {
			public PartialTwoPublicConstructor( int x, int y ) {
				X = x;
				Y = y;
			}
			public int X { get; }
			public int Y { get; }
		}
		public sealed partial record PartialTwoPublicConstructor {
			public TwoPublicConstructor( int X ) : this( x: X, y: 0 ) { }
		}

		[ReflectionSerializer]
		public sealed record MultiplePublicConstructor {
			public MultiplePublicConstructor( int x, int y, int z ) {
				X = x;
				Y = y;
				Z = z;
			}
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ MultiplePublicConstructor /**/( int x ) : this( x: x, y: 0, z: 0 ) { }
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ MultiplePublicConstructor /**/( int x, int y ) : this( x: x, y: y, z: 0 ) { }
			public int X { get; }
			public int Y { get; }
			public int Z { get; }
		}

		[ReflectionSerializer]
		public sealed record /* ReflectionSerializer_Record_NoPublicConstructor() */ NoPublicConstructor /**/ {
			internal NoPublicConstructor( int x ) {
				X = x;
			}
			public int X { get; }
		}

		[ReflectionSerializer]
		public sealed partial record /* ReflectionSerializer_Record_NoPublicConstructor() */ PartialNoPublicConstructor /**/ {
			internal PartialNoPublicConstructor( int x ) {
				X = x;
			}
			public int X { get; }
		}

		[ReflectionSerializer]
		public abstract record BaseRecord( int X );

		[ReflectionSerializer]
		public sealed record InheritedRecord( int X, int Y ) : BaseRecord( X: X );

		[ReflectionSerializer]
		public sealed record ParameterCannotBeDeserializedBecauseIngored(
				[property: ReflectionSerializer.Ignore]
				int /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Error(X) */ X /**/
			);

		[ReflectionSerializer]
		public sealed partial record PartialParameterCannotBeDeserializedBecauseIngored_WhenSameFile(
				[property: ReflectionSerializer.Ignore]
				int /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Error(X) */ X /**/
			);

		[ReflectionSerializer]
		public sealed partial record /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized_Error(x) */ PartialParameterCannotBeDeserializedBecauseIngored_WhenDifferentFile /**/ {
			[ReflectionSerializer.Ignore]
			public int X { get; }
		}
		public sealed partial record PartialParameterCannotBeDeserializedBecauseIngored_WhenDifferentFile {
			public PartialParameterCannotBeDeserializedBecauseIngored_WhenDifferentFile( int x ) {
				X = x;
			}
		}
	}

	namespace Unrelated {

		public sealed class TypeDeclarationAttribute : Attribute { }

		[TypeDeclaration]
		public sealed class UnrelatedClassAttributeUsage { }

		[TypeDeclaration]
		public sealed record UnrelatedRecordAttributeUsage { }

		[TypeDeclaration]
		public readonly struct UnrelatedStructAttributeUsage { }
	}
}
