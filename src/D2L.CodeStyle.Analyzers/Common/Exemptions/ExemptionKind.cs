namespace D2L.CodeStyle.Analyzers.Common.Exemptions {
	/// <summary>
	/// The scope of the exemption. When choosing the scope consider the
	/// trade-off between flexibility and strictness. A more strict exemption
	/// (e.g. method rather than assembly) can impede refactoring but too wide
	/// a scope (e.g. assembly rather than method) prevents more diagnostics.
	/// </summary>
	internal enum ExemptionKind {
		Assembly,
		Method,
		Class
	}
}
