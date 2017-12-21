using System;

namespace D2L.CodeStyle.Annotations {

	public static partial class DangerousMethodUsage {

		/// <summary>
		/// Indicates usages of a dangerous method are still unaudited
		/// </summary>
		[AttributeUsage(
			validOn: AttributeTargets.Method | AttributeTargets.Constructor,
			AllowMultiple = true,
			Inherited = false
		)]
		public sealed class UnauditedAttribute : Attribute {

			/// <summary>
			/// Indicates usages of a dangerous method are still unaudited
			/// </summary>
			/// <param name="declaringType">The type that declares the dangerous method.</param>
			/// <param name="methodName">The name of the dangerous method.</param>
			public UnauditedAttribute(
					Type declaringType,
					string methodName
				) {

				DeclaringType = declaringType;
				MethodName = methodName;
			}

			public Type DeclaringType { get; }
			public string MethodName { get; }
		}
	}
}
