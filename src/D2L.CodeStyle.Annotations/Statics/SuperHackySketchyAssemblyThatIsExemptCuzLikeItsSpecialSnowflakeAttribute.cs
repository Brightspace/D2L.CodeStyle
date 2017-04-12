using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
	public static partial class Statics {

		[Obsolete( "Only use this attribute as a temporary measure in assemblies. But if you use this attribute, I will come and hunt you down." )]
		[AttributeUsage( validOn: AttributeTargets.Assembly, AllowMultiple = false )]
		public sealed class SuperHackySketchyAssemblyThatIsExemptCuzLikeItsSpecialSnowflakeAttribute : Attribute {
			public readonly string LazyAuthor;
			public readonly string BogusReason;

			public SuperHackySketchyAssemblyThatIsExemptCuzLikeItsSpecialSnowflakeAttribute(
				string lazyAuthor,
				string bogusReason
			) {
				LazyAuthor = lazyAuthor;
				BogusReason = bogusReason;
			}
		}
	}
}