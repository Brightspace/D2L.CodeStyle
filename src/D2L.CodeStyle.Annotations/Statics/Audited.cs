using System;

// ReSharper disable once CheckNamespace
namespace D2L {
	public static partial class CodeStyle {
		public static partial class Statics {
			public sealed class Audited : Attribute {
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
