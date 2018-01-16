using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		/// <summary>
		/// If a class, struct or interface is marked with this annotation it
		/// means that it's type is immutable. This includes all subtypes of
		/// that type (which is trivial for structs and sealed classes.) It is
		/// always safe to add this annotation because an analyzer will check
		/// that it is valid.
		/// </summary>
		[AttributeUsage(
			validOn: AttributeTargets.Class
			       | AttributeTargets.Interface
			       | AttributeTargets.Struct
		)]
		public sealed class Immutable : Attribute {

			public Except Except { get; set; }

		}

		[Flags]
		public enum Except {

			None = 0,
			ItHasntBeenLookedAt = 1,
			ItsSketchy = 2,
			ItsStickyDataOhNooo = 4,
			WeNeedToMakeTheAnalyzerConsiderThisSafe = 8,
			ItsUgly = 16,
			ItsOnDeathRow = 32
			
		}
	}
}
