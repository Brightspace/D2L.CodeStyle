using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
    public static partial class Singletons {
        /// <summary>
        /// Indicates that a static variable is safe in a multi-tenant process
        /// </summary>
        [AttributeUsage( validOn: AttributeTargets.Class )]
        public sealed class AuditedAttribute : Attribute {
            /// <summary>
            /// Mark a singleton as safe in a multi-tenant process
            /// </summary>
            /// <param name="owner">The user who last reviewed this variable</param>
            /// <param name="auditedDate">The last time this variable was reviewed</param>
            /// <param name="rationale">A brief explaination of why this variable is safe in a multi-tenant process</param>
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
