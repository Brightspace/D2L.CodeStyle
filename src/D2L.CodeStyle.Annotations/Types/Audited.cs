using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
    public static partial class Types {
        /// <summary>
        /// Indicates that a type from another assembly is safe in a multi-tenant process.
        /// </summary>
        /// <remarks>
        /// In general, the type should be marked with <see cref="Objects.Immutable" />; 
        /// but when source code is not available, this attribute can be applied.
        /// </remarks>
        [AttributeUsage( validOn: AttributeTargets.Assembly, AllowMultiple = true )]
        public sealed class Audited : Attribute {
            /// <summary>
            /// Mark a type from another assembly as safe in a multi-tenant process
            /// </summary>
            /// <param name="owner">The user who last reviewed this type</param>
            /// <param name="auditedDate">The last time this type was reviewed</param>
            /// <param name="rationale">A brief explaination of why this type is safe in a multi-tenant process</param>
            public Audited(
                string type,
                string owner,
                string auditedDate,
                string rationale
            ) {
                Type = type;
                Owner = owner;
                AuditedDate = auditedDate;
                Rationale = rationale;
            }

            public string Type { get; }
            public string Owner { get; }
            public string AuditedDate { get; }
            public string Rationale { get; }
        }
    }
}
