// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Serialization.MustBeDeserializableAnalyzer

using System;
using System.Collections.Generic;
using D2L.LP.Serialization;
using D2L.CodeStyle.Annotations.Serialization;
using D2L.Deserialization.MustBeDeserializable.Test;

namespace D2L.LP.Serialization {

	[AttributeUsage(
		AttributeTargets.Class,
		AllowMultiple = false,
		Inherited = false
	)]
	public sealed class ReflectionSerializerAttribute : Attribute {
	}

	[AttributeUsage(
			AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface,
			AllowMultiple = false,
			Inherited = false
	)]
	public sealed class SerializerAttribute : Attribute {

		private readonly Type m_type;

		public SerializerAttribute( Type type ) {

			if( type == null ) {
				throw new ArgumentNullException( nameof( type ) );
			}

			m_type = type;
		}

		public Type Type {
			get { return m_type; }
		}
	}
}

namespace D2L.Deserialization.MustBeDeserializable.Test {

	[ReflectionSerializer]
	public class EmptyGenericWithReflectionSerializer<T> { }

	[ReflectionSerializer]
	public class EmptyMultiParamGenericWithReflectionSerializer<T, U> { }

	public class EmptyGenericNotDeserializable<T> { }

	internal interface ISerializer {
		string Serialize<[MustBeDeserializable] T>( T t );

		string SerializeUnsafe( [MustBeDeserializable] object o );
	}

	[ReflectionSerializer]
	public class EmptyReflectionSerializerType { }

	[Serializer(typeof( EmptyCustomSerializerType ) )]
	public class EmptyCustomSerializerType { }

	internal sealed class BasicTestClass {
		private readonly ISerializer m_serializer;

		public BasicTestClass( ISerializer serializer ) {
			m_serializer = serializer;
		}

		private void UnsafeGenericCall() {
			/* MustBeDeserializableRequiresAppropriateAttribute() */
			m_serializer.Serialize( new { } ) /**/;
		}

		private void UnsafeGenericCallFromParameter( object /* ArgumentShouldBeDeserializable() */ o /**/) {
			m_serializer.Serialize( o );
		}

		private void SafeGenericCallFromParameter( [MustBeDeserializable] object o ) {
			m_serializer.Serialize( o );
		}

		private void UnsafeObjectCall() {
			m_serializer.SerializeUnsafe( /* MustBeDeserializableRequiresAppropriateAttribute() */ new { } /**/);
		}

		private void UnsafeObjectCallFromParameter( object /* ArgumentShouldBeDeserializable() */ o /**/) {
			m_serializer.SerializeUnsafe( o );
		}

		private void SafeObjectCallFromParameter( [MustBeDeserializable] object o ) {
			m_serializer.SerializeUnsafe( o );
		}

		private void SafeObjectCallDueToAllowList( Dictionary<string, string> o ) {
			m_serializer.SerializeUnsafe( o );
		}

		private void SafeObjectCallDueToAllowListNullable( Dictionary<string, string>? o ) {
			m_serializer.SerializeUnsafe( o );
		}

		private void UnsafeObjectCallDueToAllowListButUnsafeType( Dictionary<string, object> /* ArgumentShouldBeDeserializable */o/**/ ) {
			m_serializer.SerializeUnsafe( o );
		}

		private void UnsafeObjectCallDueToAllowListButUnsafeTypeNullable( Dictionary<string, object?>? /* ArgumentShouldBeDeserializable */o/**/ ) {
			m_serializer.SerializeUnsafe( o );
		}
	}

	internal sealed class ComplextConcreteTypeTestClass {
		private readonly ISerializer m_serializer;

		public ComplextConcreteTypeTestClass( ISerializer serializer ) {
			m_serializer = serializer;
		}

		private void ImplicitGenericMustBeDeserializableCallWithSafeType( EmptyGenericWithReflectionSerializer<string> t ) {
			m_serializer.Serialize( t );
		}

		private void ImplicitGenericMustBeDeserializableCallFromDeserializableTypeWithUnsafeGenericParameter(
			EmptyGenericWithReflectionSerializer<EmptyGenericNotDeserializable<string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.Serialize( t );
		}

		private void ImplicitGenericMustBeDeserializableCallFromDeserializableTypeWithDeeplyNestedUnsafeGenericParameter(
			EmptyMultiParamGenericWithReflectionSerializer<string, EmptyMultiParamGenericWithReflectionSerializer<EmptyGenericNotDeserializable<string>, string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.Serialize( t );
		}

		private void ImplicitMustBeDeserializableCallWithUnsafeType( EmptyGenericNotDeserializable<string> /* ArgumentShouldBeDeserializable */ t/**/) {
			m_serializer.Serialize( t );
		}

		private void MustBeDeserializableCallWithUnsafeType( EmptyGenericNotDeserializable<string> /* ArgumentShouldBeDeserializable */o/**/ ) {
			m_serializer.SerializeUnsafe( o );
		}

		private void MustBeDeserializableCallWithUnsafeGenericParameterInTheMiddle( EmptyGenericWithReflectionSerializer<EmptyGenericNotDeserializable<string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.SerializeUnsafe( t );
		}

		private void MustBeDeserializableCallWithDeeplyNestedUnsafeGenericParameter(
			EmptyMultiParamGenericWithReflectionSerializer<string, EmptyMultiParamGenericWithReflectionSerializer<EmptyGenericNotDeserializable<string>, string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.SerializeUnsafe( t );
		}
	}

	internal sealed class SerializerTestClass {
		private readonly ISerializer m_serializer;

		public SerializerTestClass( ISerializer serializer ) {
			m_serializer = serializer;
		}

		public void SafeInvocationDueToReflectionSerializer() {
			m_serializer.SerializeUnsafe( new EmptyReflectionSerializerType() );
		}

		public void SafeGenericInvocationDueToReflectionSerializer() {
			m_serializer.Serialize<EmptyReflectionSerializerType>( new EmptyReflectionSerializerType() );
		}

		public void SafeImplicitGenericInvocationDueToReflectionSerializer() {
			m_serializer.Serialize( new EmptyReflectionSerializerType() );
		}

		public void SafeInvocationDueToSerializerAttribute() {
			m_serializer.SerializeUnsafe( new EmptyCustomSerializerType() );
		}

		public void SafeGenericInvocationDueToSerializerAttribute() {
			m_serializer.Serialize<EmptyCustomSerializerType>( new EmptyCustomSerializerType() );
		}

		public void SafeImplicitGenericInvocationDueToSerializerAttribute() {
			m_serializer.Serialize( new EmptyCustomSerializerType() );
		}
	}
}

namespace D2L.Deserialization.MustBeDeserializable.System.Types.Test {
	internal sealed class NotDeserializable { };

	[ReflectionSerializer]
	internal sealed class Deserializable { }

	internal sealed class TestClass {
		private void Dangerous([MustBeDeserializable] global::System.Type t) {}

		public void CallDangerous() {
			Dangerous( /* MustBeDeserializableRequiresAppropriateAttribute() */ typeof( NotDeserializable ) /**/ );
			Dangerous( typeof( Deserializable ) );
		}
	}
}

// the following tests check that generic classes with MustBeDeserializable enforce setting of values related to that type are set to MustBeDeserializable
namespace D2L.Deserialization.MustBeDeserializable.Recursive.Test {
	internal sealed class TestRecord {
		[ReflectionSerializer]
		private sealed record MustBeDeserializableObjectRecord<[MustBeDeserializable] T>( T Value );
		public void UnsafeCall( object o ) {
			var unsafeVal = /* MustBeDeserializableRequiresAppropriateAttribute() */ new MustBeDeserializableObjectRecord<object>( o ) /**/;
		}

		public void UnsafeGenericCall</* ArgumentShouldBeDeserializable() */T /**/>( T t ) {
			var afeVal = new MustBeDeserializableObjectRecord<T>( t );
		}

		public void SafeCall<[MustBeDeserializable] T>( T t ) {
			var safeVal = new MustBeDeserializableObjectRecord<T>( t );
		}
	}

	[ReflectionSerializer]
	public class GenericReflectionSerializerWithoutMustBeDeserializableConstructor<[MustBeDeserializable] T> {
		public GenericReflectionSerializerWithoutMustBeDeserializableConstructor(
			 T /* ArgumentShouldBeDeserializable() */ value /**/ ) {
			Unsafe = value;
		}
		public T Unsafe { get; }

		public static GenericReflectionSerializerWithoutMustBeDeserializableConstructor<X> TriggerErrorByConstruction</* ArgumentShouldBeDeserializable() */ X /**/>( X value ) {
			return new GenericReflectionSerializerWithoutMustBeDeserializableConstructor<X>( value );
		}
	}

	[ReflectionSerializer]
	public class GenericReflectionSerializerWithMustBeDeserializableConstructor<[MustBeDeserializable] T> {
		public GenericReflectionSerializerWithMustBeDeserializableConstructor(
			[MustBeDeserializable] T value ) {
			Unsafe = value;
		}

		public T Unsafe { get; }

		public static GenericReflectionSerializerWithMustBeDeserializableConstructor<X> TriggerNoErrorByConstruction<[MustBeDeserializable] X>(X value) {
			return new GenericReflectionSerializerWithMustBeDeserializableConstructor<X>( value );
		}
	}

	[ReflectionSerializer]
	public class ReflectionSerializerWithMustBeDeserializableConstructor {
		public ReflectionSerializerWithMustBeDeserializableConstructor(
			[MustBeDeserializable] object value ) {
			Unsafe = value;
		}

		public object Unsafe { get; }

		public static ReflectionSerializerWithMustBeDeserializableConstructor TriggerNoErrorByConstruction( [MustBeDeserializable] object value ) {
			return new ReflectionSerializerWithMustBeDeserializableConstructor( value );
		}

		public static ReflectionSerializerWithMustBeDeserializableConstructor TriggerErrorByConstruction( object /* ArgumentShouldBeDeserializable() */ value /**/ ) {
			return new ReflectionSerializerWithMustBeDeserializableConstructor( value );
		}
	}
}

namespace D2L.Deserialization.MustBeDeserializable.AttributesShouldBeOnInterface.Test {
	internal interface ISampleInterface {
		void NotDeserializeableTypeArgumentInInterface<T>();
		void NotDeserializeableParameterInInterface(object o);
	}

	internal sealed class EmptyChangedImplementation : ISampleInterface {
		public void NotDeserializeableTypeArgumentInInterface<[MustBeDeserializable] /* PinningAttributesShouldBeInTheInterfaceIfInImplementations() */ T /**/>() {}
		public void NotDeserializeableParameterInInterface([MustBeDeserializable] object /* PinningAttributesShouldBeInTheInterfaceIfInImplementations() */ o /**/) { }
	}

	internal sealed class EmptyMatchingImplementation : ISampleInterface {
		public void NotDeserializeableTypeArgumentInInterface<T>() { }
		public void NotDeserializeableParameterInInterface( object o ) { }
	}
}
