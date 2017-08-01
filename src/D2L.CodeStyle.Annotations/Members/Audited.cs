using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
    public static partial class Members {
		/// <summary>
		/// Indicates that a mutable or otherwise not guaranteed immutable member in a type is safe in a multi-tenant process.
		/// </summary>
		[AttributeUsage( validOn: AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false )]
        public sealed class Audited : Attribute {
            /// <summary>
            /// Mark a mutable or otherwise not guaranteed immutable member as safe in a multi-tenant process
            /// </summary>
            /// <param name="owner">The user who last reviewed this member</param>
            /// <param name="auditedDate">The last time this member was reviewed</param>
            /// <param name="rationale">A brief explaination of why this member is safe in a multi-tenant process</param>
            public Audited(
                string owner,
                string auditedDate,
                string rationale
            ) {
                Owner = owner;
                AuditedDate = auditedDate;
                Rationale = rationale;
            }

            public string Owner { get; }
            public string AuditedDate { get; }
            public string Rationale { get; }
        }
    }
}
