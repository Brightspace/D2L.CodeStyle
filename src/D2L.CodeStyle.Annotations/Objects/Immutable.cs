using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public static partial class Objects {

		public abstract class ImmutableAttributeBase : Attribute {}

		/// <summary>
		/// If a class, struct or interface is marked with this annotation it
		/// means that it's type is immutable. This includes all subtypes of
		/// that type (which is trivial for structs and sealed classes.) It is
		/// always safe to add this annotation because an analyzer will check
		/// that it is valid.
		/// </summary>
		[AttributeUsage(
			validOn: AttributeTargets.Class
			       | AttributeTargets.Interface
			       | AttributeTargets.Struct
			       | AttributeTargets.GenericParameter
		)]
		public sealed class Immutable : ImmutableAttributeBase { }

		/// <summary>
		/// If a class is marked with this annotation it means that it is immutable
		/// but other mutable classes may sub-class it.
		/// It is always safe to add this annotation because an analyzer will check
		/// that it is valid.
		[AttributeUsage( validOn: AttributeTargets.Class )]
		public sealed class ImmutableBaseClassAttribute : ImmutableAttributeBase { }

		/// <summary>
		/// If a class, struct or interface is marked with this annotation it
		/// means that it's type is immutable if all type arguments marked with [OnlyIf]
		/// are themselves immutable.
		/// </summary>
		[AttributeUsage(
			validOn: AttributeTargets.Class
			       | AttributeTargets.Interface
			       | AttributeTargets.Struct
		)]
		public sealed class ConditionallyImmutable : ImmutableAttributeBase {

			[AttributeUsage( validOn: AttributeTargets.GenericParameter )]
			public sealed class OnlyIf : ImmutableAttributeBase { }

		}

	}
}
