namespace D2L.CodeStyle.Analyzers.Immutability {
	/// <summary>
	/// Determines what kind of immutable a type has.
	/// </summary>
	internal enum ImmutabilityScope {
		None,
		Self,
		SelfAndChildren
	}
}
