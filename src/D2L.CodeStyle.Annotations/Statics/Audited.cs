using System;

// ReSharper disable once CheckNamespace
namespace D2L {
	public static partial class CodeStyle {
		public static partial class Statics {
			/// <summary>
			/// Indicates that a static variable is safe in a multi-tenant process
			/// </summary>
			[AttributeUsage( validOn: AttributeTargets.Field )]
			public sealed class Audited : Attribute {
				/// <summary>
				/// Mark a static variable as safe in a multi-tenant process
				/// </summary>
				/// <param name="owner">The user who last reviewed this variable</param>
				/// <param name="auditedDate">The last time this variable was reviewed</param>
				/// <param name="rationale">A brief explaination of why this variable is safe in a multi-tenant process</param>
				public Audited(
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
}
