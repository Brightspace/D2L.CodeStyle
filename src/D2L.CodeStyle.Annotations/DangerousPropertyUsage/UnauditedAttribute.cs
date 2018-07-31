using System;

namespace D2L.CodeStyle.Annotations {

	public static partial class DangerousPropertyUsage {

		/// <summary>
		/// Indicates usages of a dangerous property are still unaudited
		/// </summary>
		[AttributeUsage(
			validOn: AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property,
			AllowMultiple = true,
			Inherited = false
		)]
		public sealed class UnauditedAttribute : Attribute {

			/// <summary>
			/// Indicates usages of a dangerous property are still unaudited
			/// </summary>
			/// <param name="declaringType">The type that declares the dangerous property.</param>
			/// <param name="propertyName">The name of the dangerous property.</param>
			public UnauditedAttribute(
					Type declaringType,
					string propertyName
				) {

				DeclaringType = declaringType;
				PropertyName = propertyName;
			}

			public Type DeclaringType { get; }
			public string PropertyName { get; }
		}
	}
}
