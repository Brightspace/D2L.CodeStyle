// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public enum Because {
		/// <summary>
		/// DEPRECATED: Things marked with this attribute should be recategorized.
		/// </summary>
		ItHasntBeenLookedAt = 1,

		/// <summary>
		/// DEPRECATED: Things marked with this attribute should be recategorized.
		/// This was used for things that are ugly code, possibly ugly enough to
		/// be an undiff problem. We need to better categorize these so that we can
		/// prioritize work that gets us closer to undiff.
		/// </summary>
		ItsSketchy = 2,

		/// <summary>
		/// These are blockers for undiff.
		/// </summary>
		ItsStickyDataOhNooo = 3,

		/// <summary>
		/// These are things which could be analyzed as safe but require improvements
		/// to the analyzer. An alternative is [Audited(...)] when we don't
		/// expect an improvements to the analyzer to pass this variable.
		/// </summary>
		WeNeedToMakeTheAnalyzerConsiderThisSafe = 4,

		/// <summary>
		/// This code is ugly and we don't feel comfortable making it
		/// [Audited(...)] (which is a long-term acceptable bucket) we
		/// are not prioritizing this work for undiff.
		/// </summary>
		ItsUgly = 5,

		/// <summary>
		/// This code won't ship or be enabled for undiff clients. It
		/// constitutes a feature that is not relevant for them, possibly
		/// because there is a newer parallel implementation or it is not
		/// compatible with next-gen hosting.
		/// </summary>
		ItsOnDeathRow = 6
	}
}
