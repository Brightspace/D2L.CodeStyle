using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public static partial class Mutability {
		/// <summary>
		/// Indicates some mutable state is safe in an otherwise immutable type
		/// </summary>
		[AttributeUsage( validOn: AttributeTargets.Field | AttributeTargets.Property )]
		public sealed class AuditedAttribute : Attribute {
			/// <summary>
			/// Mark some mutable state as safe in an otherwise immutable type
			/// </summary>
			/// <param name="owner">The user who last reviewed this state</param>
			/// <param name="auditedDate">The last time this variable was reviewed</param>
			/// <param name="rationale">A brief explaination of why this state is safe.</param>
			public AuditedAttribute(
				string owner,
				string auditedDate,
				string rationale
			) {
				Owner = owner;
				AuditedDate = auditedDate;
				Rationale = rationale;
			}

			public string Owner { get; private set; }
			public string AuditedDate { get; private set; }
			public string Rationale { get; private set; }
		}
	}
}
