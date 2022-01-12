// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Serialization.ReflectionSerializerAnalyzer

using System;
using D2L.LP.Serialization;

namespace D2L.LP.Serialization {

	[AttributeUsage(
		validOn: AttributeTargets.Class,
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

	namespace Classes {

		[ReflectionSerializer]
		public sealed class Empty { }

		[ReflectionSerializer]
		public sealed class PublicConstructorOnly {
			public PublicConstructorOnly( int x ) {
				X = x;
			}
			public int X { get; }
		}

		[ReflectionSerializer]
		public sealed class PublicAndInternalConstructor {
			public PublicAndInternalConstructor( int x, int y ) {
				X = x;
				Y = y;
			}
			internal PublicAndInternalConstructor( int x )
				: this( x: x, y: 0 ) {
			}
			public int X { get; }
			public int Y { get; }
		}

		[ReflectionSerializer]
		public sealed class /* ReflectionSerializer_Class_NoPublicConstructor() */ NoPublicConstructors /**/ {
			internal NoPublicConstructors( int x ) {
				X = x;
			}
			public int X { get; }
		}

		[ReflectionSerializer]
		public sealed class MultiplePublicConstructors {
			public MultiplePublicConstructors( int x, int y ) {
				X = x;
				Y = y;
			}
			public /* ReflectionSerializer_Class_MultiplePublicConstructors() */ MultiplePublicConstructors /**/ ( int x )
				: this( x: x, y: 0 ) {
			}
			public int X { get; }
			public int Y { get; }
		}

		[ReflectionSerializer]
		public sealed class ParameterCannotBeDeserialized_BecauseNameMismatch {
			public ParameterCannotBeDeserialized_BecauseNameMismatch(
				int /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized(value) */ value /**/
			) {
				X = value;
			}
			public int X { get; }
        }

		[ReflectionSerializer]
		public sealed class ParameterCannotBeDeserialized_And_MultipleConstructors {
			public ParameterCannotBeDeserialized_And_MultipleConstructors(
				int /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized(value) */ value /**/
			) : this( x: value, y: 0 ) {}
			public /* ReflectionSerializer_Class_MultiplePublicConstructors() */ ParameterCannotBeDeserialized_And_MultipleConstructors /**/ ( int x, int y ) {
				X = x;
				Y = y;
			}
			public int X { get; }
			public int Y { get; }
		}

		[ReflectionSerializer]
        public sealed class GetterSetter {
			public int X { get; set; }
			public int Y { get; set; }
		}
    }

    namespace Records {

		[ReflectionSerializer]
		public sealed record Empty();

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
		public sealed record PublicAndInternalConstructor {
			public PublicAndInternalConstructor( int x, int y ) {
				X = x;
				Y = y;
			}
			internal PublicAndInternalConstructor( int x )
				: this( x: x, y: 0 ) {
			}
			public int X { get; }
			public int Y { get; }
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
		public sealed partial record PartialPrimaryAndPublicConstructorWithPrimaryAttributed ( int X, int Y );
		public sealed partial record PartialPrimaryAndPublicConstructorWithPrimaryAttributed {
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PartialPrimaryAndPublicConstructorWithPrimaryAttributed /**/ (int x ) : this( X: x, Y: 0 ) { }
		}

		public sealed partial record PartialPrimaryAndPublicConstructorWithPublicAttributed( int X, int Y );
		[ReflectionSerializer]
		public sealed partial record PartialPrimaryAndPublicConstructorWithPublicAttributed {
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PartialPrimaryAndPublicConstructorWithPublicAttributed /**/(int x ) : this( X: x, Y: 0 ) { }
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
		public sealed partial record PartialTwoPublicConstructor {
			public PartialTwoPublicConstructor( int x, int y ) {
				X = x;
				Y = y;
			}
			public int X { get; }
			public int Y { get; }
		}
		public sealed partial record PartialTwoPublicConstructor {
			public /* ReflectionSerializer_Record_MultiplePublicConstructors() */ PartialTwoPublicConstructor /**/( int X ) : this( x: X, y: 0 ) { }
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
				int /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized(X) */ X /**/
			);

		[ReflectionSerializer]
		public sealed partial record PartialParameterCannotBeDeserializedBecauseIngored_WhenSameFile(
				[property: ReflectionSerializer.Ignore]
				int /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized(X) */ X /**/
			);

		[ReflectionSerializer]
		public sealed partial record PartialParameterCannotBeDeserializedBecauseIngored_WhenDifferentFile {
			[ReflectionSerializer.Ignore]
			public int X { get; }
		}
		public sealed partial record PartialParameterCannotBeDeserializedBecauseIngored_WhenDifferentFile {
			public PartialParameterCannotBeDeserializedBecauseIngored_WhenDifferentFile( int /* ReflectionSerializer_ConstructorParameterCannotBeDeserialized(x) */ x /**/ ) {
				X = x;
			}
		}

		[ReflectionSerializer]
		public record EmptyRecord();

		[ReflectionSerializer]
		public partial record PartialEmptyRecord_AttributeInSameFile();

		public partial record PartialEmptyRecord_AttributeInDifferentFile();
		[ReflectionSerializer]
		public partial record PartialEmptyRecord_AttributeInDifferentFile { }

		[ReflectionSerializer]
		public sealed record GetterInitOnlySetter {
			public int X { get; /* ReflectionSerializer_Record_InitOnlySetter */ init /**/; }
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
