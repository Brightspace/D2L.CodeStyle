// analyzer: D2L.CodeStyle.Analyzers.Language.ImmutableGenericDeclarationAnalyzer

using System;
using System.Collections.Generic;

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public abstract class ImmutableAttributeBase : Attribute {
			public string Except { get; set; }
		}
		[AttributeUsage(
			validOn: AttributeTargets.Class
				   | AttributeTargets.Interface
				   | AttributeTargets.Struct
				   | AttributeTargets.GenericParameter
		)]
		public sealed class Immutable : ImmutableAttributeBase { }
		public sealed class ImmutableBaseClassAttribute : ImmutableAttributeBase { }
	}
}

namespace SpecTests {
	using D2L.CodeStyle.Annotations;

	class ConcreteGenericTests {

		enum TheEnum {
			Unknown,
			Known
		}

		[Objects.Immutable]
		class ImmutableClass {
			private readonly string m_ImmutableClass;
		}

		class MutableClass {
			private string m_MutableClass;
		}

		[Objects.Immutable]
		class GenericBase<[Objects.Immutable] T> {
			private readonly T m_T;
		}

		class ConcreteMutableGeneric :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ GenericBase<MutableClass> /**/{
		}

		class ConcreteImmutableGeneric : GenericBase<ImmutableClass> {
		}

		class ConcreteKnownImmutableGeneric : GenericBase<bool> {
		}

		[Objects.Immutable]
		class MultiGenericBase<S, [Objects.Immutable] T> {
			private readonly T m_T;
		}

		class ConcreteMutableMultiGeneric :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ MultiGenericBase<MutableClass, MutableClass> /**/ {
		}

		class ConcreteMixedMutableMultiGeneric :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ MultiGenericBase<ImmutableClass, MutableClass> /**/ {
		}

		class ConcreteMixedImmutableMultiGeneric : MultiGenericBase<MutableClass, ImmutableClass> {
		}

		class ConcreteImmutableMultiGeneric : MultiGenericBase<ImmutableClass, ImmutableClass> {
		}

		class ConcreteKnownImmutableMultiGeneric : MultiGenericBase<bool, string> {
		}

		[Objects.Immutable]
		interface ImmutableInterface<[Objects.Immutable] T> {
		}

		class MutableImplementation :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ ImmutableInterface<MutableClass> /**/ {
		}

		class ImmutableImplementation : ImmutableInterface<ImmutableClass> {
		}

		struct MutableStructImplementation :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ ImmutableInterface<MutableClass> /**/ {
		}

		struct ImmutableStructImplementation : ImmutableInterface<ImmutableClass> {
		}

		struct KnownImmutableStructImplementation : ImmutableInterface<bool> {
		}

		[Objects.Immutable]
		interface MultiInterface<S, [Objects.Immutable] T> {
		}

		class MutableMultiImplementation :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ MultiInterface<MutableClass, MutableClass> /**/ {
		}

		class MixedMutableMultiImplementation :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ MultiInterface<ImmutableClass, MutableClass> /**/ {
		}

		class ImmutableMultiImplementation : MultiInterface<ImmutableClass, ImmutableClass> {
		}

		class MixedImmutableMultiImplementation : MultiInterface<MutableClass, ImmutableClass> {
		}

		struct MutableStructMultiImplementation :/* GenericArgumentTypeMustBeImmutable(MutableClass) */ MultiInterface<MutableClass, MutableClass> /**/ {
		}

		struct ImmutableStructMultiImplementation : MultiInterface<ImmutableClass, ImmutableClass> {
		}

		struct MixedImmutableStructMultiImplementation : MultiInterface<MutableClass, ImmutableClass> {
		}

		struct KnownMixedImmutableStructMultiImplementation : MultiInterface<bool, bool> {
		}

		class ClassMutableProperty {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ GenericBase<MutableClass> /**/ Prop { get; set; }
		}

		class ClassImmutableProperty {
			GenericBase<ImmutableClass> Prop { get; set; }
		}

		class ClassMutableField {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ GenericBase<MutableClass> /**/ m_field;
		}

		class ClassImmutableField {
			GenericBase<ImmutableClass> m_field;
		}

		class ClassMutableInterfaceProperty {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ ImmutableInterface<MutableClass> /**/ Prop { get; set; }
		}

		class ClassImmutableInterfaceProperty {
			ImmutableInterface<ImmutableClass> Prop { get; set; }
		}

		struct StructMutableClassProperty {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ GenericBase<MutableClass> /**/ Prop { get; set; }
		}

		struct StructImmutableClassProperty {
			GenericBase<ImmutableClass> Prop { get; set; }
		}

		struct StructMutableClassField {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ GenericBase<MutableClass> /**/ m_field;
		}

		struct StructImmutableClassField {
			GenericBase<ImmutableClass> m_field;
		}

		struct StructMutableInterfaceProperty {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ ImmutableInterface<MutableClass> /**/ Prop { get; set; }
		}

		struct StructImmutableInterfaceProperty {
			ImmutableInterface<ImmutableClass> Prop { get; set; }
		}

		struct StructMutableInterfaceField {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ ImmutableInterface<MutableClass> /**/ m_field;
		}

		struct StructImmutableInterfaceField {
			ImmutableInterface<ImmutableClass> m_field;
		}

		interface InterfaceMutableClassProperty {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ GenericBase<MutableClass> /**/ Prop { get; set; }
		}

		interface InterfaceImmutableClassProperty {
			GenericBase<ImmutableClass> Prop { get; set; }
		}

		class ClassKnownImmutableProperty {
			GenericBase<bool> Prop { get; set; }
		}

		class ClassKnownImmutableInterfaceProperty {
			ImmutableInterface<bool> Prop { get; set; }
		}

		class ClassKnownImmutableField {
			GenericBase<bool> m_field;
		}

		struct StructKnownImmutableClassProperty {
			GenericBase<bool> Prop { get; set; }
		}

		struct StructKnownImmutableInterfaceProperty {
			ImmutableInterface<bool> Prop { get; set; }
		}

		struct StructKnownImmutableClassField {
			GenericBase<bool> m_field;
		}

		struct StructKnownImmutableInterfaceField {
			ImmutableInterface<bool> m_field;
		}

		[Objects.Immutable]
		abstract class AbstractGenericBase<[Objects.Immutable] T> {
		}

		class GenericMethodType : AbstractGenericBase<bool> {
		}

		[Objects.Immutable]
		interface ConstrainedInterface<[Objects.Immutable] TDefinition, [Objects.Immutable] TValue>
			where TDefinition : AbstractGenericBase<TValue>, new() {
		}

		[Objects.Immutable]
		class GenericMethodClass {

			private ConstrainedInterface<GenericMethodType, bool> m_flag;

			public void ClassMethod() {

				LocalMethod( m_flag );

				void LocalMethod<TDefinition>(
					ConstrainedInterface<TDefinition, bool> flag
				) where TDefinition : AbstractGenericBase<bool>, new() {
				}
			}
		}

		class ImmutableGenericEnum {
			ImmutableInterface<TheEnum> Prop { get; set; }
		}

		class ImmutableGenericBool {
			ImmutableInterface<bool> Prop { get; set; }
		}

		class ImmutableNullableLong {
			ImmutableInterface<long?> Prop { get; set; }
		}

		class ImmutableEnumerableNullableLong {
			ImmutableInterface<IEnumerable<long?>> Prop { get; set; }
		}

		class ImmutableDictionaryStringNullableLong {
			ImmutableInterface<IReadOnlyDictionary<string, long?>> Prop { get; set; }
		}

		class ImmutableDictionaryStringMutableClass {
			/* GenericArgumentTypeMustBeImmutable(MutableClass) */ ImmutableInterface<IReadOnlyDictionary<string, MutableClass>> /**/ Prop { get; set; }
		}
	}
}