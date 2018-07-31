using System;

namespace D2L.CodeStyle.Annotations {

	public static partial class DangerousPropertyUsage {

		/// <summary>
		/// Indicates usages of a dangerous property have been declared as safe
		/// </summary>
		[AttributeUsage(
			validOn: AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property,
			AllowMultiple = true,
			Inherited = false
		)]
		public sealed class AuditedAttribute : Attribute {

			/// <summary>
			/// Indicates usages of a dangerous property have been declared as safe
			/// </summary>
			/// <param name="declaringType">The type that declares the dangerous property.</param>
			/// <param name="propertyName">The name of the dangerous property.</param>
			/// <param name="owner">The user who last reviewed this property usage.</param>
			/// <param name="auditedDate">The last time this property usage was reviewed.</param>
			/// <param name="rationale">A brief explaination of why this property usage is safe.</param>
			public AuditedAttribute(
					Type declaringType,
					string propertyName,
					string owner,
					string auditedDate,
					string rationale
				) {

				DeclaringType = declaringType;
				PropertyName = propertyName;
				Owner = owner;
				AuditedDate = auditedDate;
				Rationale = rationale;
			}

			public Type DeclaringType { get; }
			public string PropertyName { get; }
			public string Owner { get; }
			public string AuditedDate { get; }
			public string Rationale { get; }
		}
	}
}
