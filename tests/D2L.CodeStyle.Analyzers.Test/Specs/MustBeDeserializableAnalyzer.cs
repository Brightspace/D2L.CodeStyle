// analyzer: D2L.CodeStyle.Analyzers.Pinning.MustBeDeserializableAnalyzer

using System.Collections.Generic;
using D2L.CodeStyle.Annotations.Pinning;
using D2L.Pinning.MustBeDeserializable.Test;

namespace D2L.Pinning.MustBeDeserializable.Test {
	public class Constants {
		public const string AssemblyName = "PinnedAttributeAnalyzer";
	}

	[Pinned(fullyQualifiedName: "D2L.Pinning.MustBeDeserializable.Test.EmptyGenericPinnedRecursively<T>", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true)]
	public class EmptyGenericPinnedRecursively<T> {}

	[Pinned(fullyQualifiedName: "D2L.Pinning.MustBeDeserializable.Test.EmptyMultiParamGenericPinnedRecursively<T,U>", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true)]
	public class EmptyMultiParamGenericPinnedRecursively<T,U> {}

	public class EmptyGenericNotPinned<T> {}

	internal interface ISerializer {
		string Serialize<[MustBeDeserializable] T>( T t );

		string SerializeUnsafe([MustBeDeserializable] object o );

		string FormatType<[MustBePinned] T>( T t );

		string FormatTypeUnsafe([MustBePinned] object o );
	}


	[Pinned(fullyQualifiedName: "D2L.Pinning.Recursive.Test.EmptyPinnedRecursively", assembly: "RecursivelyPinnedAnalyzer", pinnedRecursively: true)]
	public class EmptyPinnedRecursively {}

	internal sealed class BasicTestClass {
		private readonly ISerializer m_serializer;

		public BasicTestClass( ISerializer serializer ) {
			m_serializer = serializer;
		}

		private void UnsafeGenericCall() {
			/* MustBeDeserializableRequiresRecursivelyPinned() */ m_serializer.Serialize( new {} ) /**/;
		}

		private void UnsafeGenericCallFromParameter( object /* ArgumentShouldBeDeserializable() */ o /**/) {
			 m_serializer.Serialize( o );
		}

		private void SafeGenericCallFromParameter([MustBeDeserializable] object o) {
			m_serializer.Serialize( o );
		}

		private void UnsafeObjectCall() {
			m_serializer.SerializeUnsafe( /* MustBeDeserializableRequiresRecursivelyPinned() */ new {} /**/);
		}

		private void UnsafeObjectCallFromParameter( object /* ArgumentShouldBeDeserializable() */ o /**/) {
			m_serializer.SerializeUnsafe( o );
		}

		private void SafeObjectCallFromParameter([MustBeDeserializable] object o) {
			m_serializer.SerializeUnsafe( o );
		}

		private void UnsafeGenericMustBePinnedCall() {
			/* MustBePinnedRequiresPinned() */ m_serializer.FormatType<object>( new {} )/**/;
		}

		private void UnsafeGenericMustBePinnedCallFromParameter( object /* ArgumentShouldBeMustBePinned() */ o /**/) {
			m_serializer.FormatType( o );
		}

		private void SafeGenericMustBePinnedCallFromPinnedParameter<[MustBePinned] T>( T t) {
			m_serializer.FormatType<T>( t );
		}

		private void SafeImplicitGenericMustBePinnedCallFromDeserializableParameter<[MustBeDeserializable]T>( T t) {
			m_serializer.FormatType( t );
		}

		private void UnsafeMustBePinnedCall() {
			/* MustBePinnedRequiresPinned() */ m_serializer.FormatType( new {} )/**/;
		}

		private void UnsafeMustBePinnedCallFromParameter( object /* ArgumentShouldBeMustBePinned() */ o /**/) {
			m_serializer.FormatType( o );
		}

		private void SafeMustBePinnedCallFromParameter([MustBePinned] object o) {
			m_serializer.FormatTypeUnsafe( o );
		}

		private void SafeMustBePinnedCallFromDeserializableParameter([MustBeDeserializable] object o) {
			m_serializer.FormatTypeUnsafe( o );
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

		private void SafeImplicitGenericMustBeDeserializableCallFromPinnedType(EmptyGenericPinnedRecursively<string> t) {
			m_serializer.Serialize( t );
		}

		private void ImplicitGenericMustBeDeserializableCallFromPinnedTypeWithUnpinnedChild(EmptyGenericPinnedRecursively<EmptyGenericNotPinned<string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.Serialize( t );
		}

		private void ImplicitGenericMustBeDeserializableCallFromPinnedTypeWithDeeplyNestedUnpinnedChild(EmptyMultiParamGenericPinnedRecursively<string, EmptyMultiParamGenericPinnedRecursively<EmptyGenericNotPinned<string>, string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.Serialize( t );
		}

		private void SafeImplicitGenericMustBeDeserializableCallFromPinnedType(EmptyGenericNotPinned<string> /* ArgumentShouldBeDeserializable */ t/**/) {
			m_serializer.Serialize( t );
		}

		private void SafeImplicitGenericMustBePinnedCallFromPinnedType(EmptyGenericPinnedRecursively<string> t) {
			m_serializer.FormatType( t );
		}

		private void ImplicitGenericMustBePinnedCallFromPinnedTypeWithUnpinnedChild(EmptyGenericPinnedRecursively<EmptyGenericNotPinned<string>>/* ArgumentShouldBeMustBePinned */ t /**/) {
			m_serializer.FormatType( t );
		}

		private void ImplicitGenericMustBePinnedCallFromPinnedTypeWithDeeplyNestedUnpinnedChild(EmptyMultiParamGenericPinnedRecursively<string, EmptyMultiParamGenericPinnedRecursively<EmptyGenericNotPinned<string>, string>>/* ArgumentShouldBeMustBePinned */ t /**/) {
			m_serializer.FormatType( t );
		}

		private void SafeImplicitGenericMustBePinnedCallFromPinnedType(EmptyGenericNotPinned<string> /* ArgumentShouldBeMustBePinned */ t/**/) {
			m_serializer.FormatType( t );
		}

		private void SafeObjectCallDueToLackOfPinning( EmptyGenericNotPinned<string> /* ArgumentShouldBeDeserializable */o/**/ ) {
			m_serializer.SerializeUnsafe( o );
		}

		private void UnsafeObjectCallMustBePinnedCallFromPinnedTypeWithUnpinnedChild(EmptyGenericPinnedRecursively<EmptyGenericNotPinned<string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.SerializeUnsafe( t );
		}

		private void UnsafeObjectCallMustBePinnedCallFromPinnedTypeWithDeeplyNestedUnpinnedChild(EmptyMultiParamGenericPinnedRecursively<string, EmptyMultiParamGenericPinnedRecursively<EmptyGenericNotPinned<string>, string>>/* ArgumentShouldBeDeserializable */ t /**/) {
			m_serializer.SerializeUnsafe( t );
		}

		private void SafeImplicitGenericCallDueToLackOfPinning( EmptyGenericNotPinned<string> /* ArgumentShouldBeMustBePinned */o/**/ ) {
			m_serializer.FormatTypeUnsafe( o );
		}

		private void UnsafeImplicitGenericCallMustBePinnedCallFromPinnedTypeWithUnpinnedChild(EmptyGenericPinnedRecursively<EmptyGenericNotPinned<string>>/* ArgumentShouldBeMustBePinned */ t /**/) {
			m_serializer.FormatTypeUnsafe( t );
		}

		private void UnsafeImplicitGenericCallMustBePinnedCallFromPinnedTypeWithDeeplyNestedUnpinnedChild(EmptyMultiParamGenericPinnedRecursively<string, EmptyMultiParamGenericPinnedRecursively<EmptyGenericNotPinned<string>, string>>/* ArgumentShouldBeMustBePinned */ t /**/) {
			m_serializer.FormatTypeUnsafe( t );
		}
	}
}

namespace D2L.Pinning.MustBeDeserializable.System.Types.Test {
	internal sealed class NotPinned {};

	[Pinned( "D2L.Pinning.MustBeDeserializable.System.Types.Test.Pinned", "", true)]
	internal sealed class Pinned {}

	internal sealed class TestClass {
		private void Dangerous([MustBePinned] global::System.Type t) {}

		public void CallDangerous() {
			Dangerous( /* MustBePinnedRequiresPinned() */ typeof(NotPinned) /**/ );
			Dangerous( typeof(Pinned) );
		}
	}
}

// the following tests check that generic classes with MustBeDeserializable enforce setting of values related to that type are set to MustBeDeserializable
namespace D2L.Pinning.MustBeDeserializable.Recursive.Test {
	internal sealed class TestRecord {
		[Pinned( fullyQualifiedName: "D2L.Pinning.MustBeDeserializable.Recursive.Test.MustBeDeserializableObjectRecord<T>", assembly: Constants.AssemblyName, pinnedRecursively: true )]
		private sealed record MustBeDeserializableObjectRecord<[MustBeDeserializable] T>( T Value );
		public void UnsafeCall( object o ) {
			var unsafeVal = /* MustBeDeserializableRequiresRecursivelyPinned() */ new MustBeDeserializableObjectRecord<object>( o ) /**/;
		}

		public void UnsafeGenericCall</* ArgumentShouldBeDeserializable() */T /**/>( T t ) {
			var afeVal = new MustBeDeserializableObjectRecord<T>( t );
		}

		public void SafeCall<[MustBeDeserializable] T>( T t ) {
			var safeVal = new MustBeDeserializableObjectRecord<T>( t );
		}
	}

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.GenericPinnedRecursivelyWithoutMustBeDeserializableConstructor<T>", assembly: Constants.AssemblyName, pinnedRecursively: true )]
	public class GenericPinnedRecursivelyWithoutMustBeDeserializableConstructor<[MustBeDeserializable] T> {
		public GenericPinnedRecursivelyWithoutMustBeDeserializableConstructor(
			 T /* ArgumentShouldBeDeserializable() */ value /**/ ) {
			Unsafe = value;
		}
		public T Unsafe { get; }

		public static GenericPinnedRecursivelyWithoutMustBeDeserializableConstructor<X> TriggerErrorByConstruction</* ArgumentShouldBeDeserializable() */ X /**/>( X value ) {
			return new GenericPinnedRecursivelyWithoutMustBeDeserializableConstructor<X>( value );
		}
	}

	[Pinned( fullyQualifiedName: "D2L.Pinning.Recursive.Test.GenericPinnedRecursivelyWithoutMustBePinnedConstructor<T>", assembly: Constants.AssemblyName, pinnedRecursively: true )]
	public class GenericPinnedRecursivelyWithMustBeDeserializableConstructor<[MustBeDeserializable] T> {
		public GenericPinnedRecursivelyWithMustBeDeserializableConstructor(
			[MustBeDeserializable] T value ) {
			Unsafe = value;
		}

		public T Unsafe { get; }

		public static GenericPinnedRecursivelyWithMustBeDeserializableConstructor<X> TriggerErrorByConstruction<[MustBeDeserializable] X>(X value) {
			return new GenericPinnedRecursivelyWithMustBeDeserializableConstructor<X>( value );
		}
	}
}

namespace D2L.Pinning.MustBeDeserializable.AttributesShouldBeOnInterface.Test {
	internal interface ISampleInterface {
		void UnpinnedTypeArgumentInInterface<T>();
		void NotDeserializeableTypeArgumentInInterface<T>();
		void UnpinnedParameterInInterface(object o);
		void NotDeserializeableParameterInInterface(object o);
	}

	internal sealed class EmptyChangedImplementation : ISampleInterface {
		public void UnpinnedTypeArgumentInInterface<[MustBePinned] /* PinningAttributesShouldBeInTheInterfaceIfInImplementations() */ T /**/>() {}
		public void NotDeserializeableTypeArgumentInInterface<[MustBeDeserializable] /* PinningAttributesShouldBeInTheInterfaceIfInImplementations() */ T /**/>() {}
		public void UnpinnedParameterInInterface([MustBePinned] object /* PinningAttributesShouldBeInTheInterfaceIfInImplementations() */ o /**/ ) { }
		public void NotDeserializeableParameterInInterface([MustBeDeserializable] object /* PinningAttributesShouldBeInTheInterfaceIfInImplementations() */ o /**/) { }
	}

	internal sealed class EmptyMatchingImplementation : ISampleInterface {
		public void UnpinnedTypeArgumentInInterface<T>() { }
		public void NotDeserializeableTypeArgumentInInterface<T>() { }
		public void UnpinnedParameterInInterface( object o ) { }
		public void NotDeserializeableParameterInInterface( object o ) { }
	}
}
