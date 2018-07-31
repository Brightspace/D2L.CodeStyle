using System;

namespace D2L.CodeStyle.Annotations {

	public static partial class DangerousMethodUsage {

		/// <summary>
		/// Indicates usages of a dangerous method have been declared as safe
		/// </summary>
		[AttributeUsage(
			validOn: AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property,
			AllowMultiple = true,
			Inherited = false
		)]
		public sealed class AuditedAttribute : Attribute {

			/// <summary>
			/// Indicates usages of a dangerous method have been declared as safe
			/// </summary>
			/// <param name="declaringType">The type that declares the dangerous method.</param>
			/// <param name="methodName">The name of the dangerous method.</param>
			/// <param name="owner">The user who last reviewed this method usage.</param>
			/// <param name="auditedDate">The last time this method usage was reviewed.</param>
			/// <param name="rationale">A brief explaination of why this method usage is safe.</param>
			public AuditedAttribute(
					Type declaringType,
					string methodName,
					string owner,
					string auditedDate,
					string rationale
				) {

				DeclaringType = declaringType;
				MethodName = methodName;
				Owner = owner;
				AuditedDate = auditedDate;
				Rationale = rationale;
			}

			public Type DeclaringType { get; }
			public string MethodName { get; }
			public string Owner { get; }
			public string AuditedDate { get; }
			public string Rationale { get; }
		}
	}
}
