// analyzer: D2L.CodeStyle.Analyzers.Immutability.ExplicitImmutableAttributeTransitivityAnalyzer

using System;
using static D2L.CodeStyle.Annotations.Objects;

namespace D2L.CodeStyle.Annotations {
	public static class Objects {
		public abstract class ImmutableAttributeBase : Attribute { }
		[AttributeUsage(
			validOn: AttributeTargets.Class
				   | AttributeTargets.Interface
				   | AttributeTargets.Struct
				   | AttributeTargets.GenericParameter
		)]
		public sealed class Immutable : ImmutableAttributeBase { }

		[AttributeUsage(
			validOn: AttributeTargets.Class
				   | AttributeTargets.Interface
				   | AttributeTargets.Struct
		)]
		public sealed class ConditionallyImmutable : ImmutableAttributeBase {
			[AttributeUsage( validOn: AttributeTargets.GenericParameter )]
			public sealed class OnlyIf : ImmutableAttributeBase { }

		}
	}
}

namespace Tests {
	public interface IVanilla { }
	public interface IVanilla2 { }
	public class VanillaBase : IVanilla { }
	public class VanillaDerived : VanillaBase { }
	public sealed class VanillaDerived2 : VanillaDerived, IVanilla2 { }
	public struct VanillaStruct : IVanilla, IVanilla2 { }

	[Immutable]
	public interface ISomethingImmutable { }

	[Immutable]
	public class HappyImplementor : ISomethingImmutable { }

	[Immutable]
	public sealed class HappyDeriver : HappyImplementor { }

	[Immutable]
	public struct HappyStructImplementor : ISomethingImmutable { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(Tests.SadImplementor, , interface, Tests.ISomethingImmutable) */ SadImplementor /**/
		: ISomethingImmutable { }

	public struct
		/* MissingTransitiveImmutableAttribute(Tests.SadStructImplementor, , interface, Tests.ISomethingImmutable) */ SadStructImplementor /**/
		: ISomethingImmutable { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(Tests.SadDeriver, , base class, Tests.HappyImplementor) */ SadDeriver /**/
		: HappyImplementor { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(Tests.SadImplementor2, , interface, Tests.ISomethingImmutable) */ SadImplementor2 /**/
		: VanillaBase, IVanilla, IVanilla2, ISomethingImmutable { }

	public sealed class
		/* MissingTransitiveImmutableAttribute(Tests.SadImplementor3, , base class, Tests.HappyImplementor) */ SadImplementor3 /**/
		: HappyImplementor, IVanilla, IVanilla2 { }

	public interface
		/* MissingTransitiveImmutableAttribute(Tests.SadExtender, , interface, Tests.ISomethingImmutable) */ SadExtender /**/
		: ISomethingImmutable { }

	// This won't emit an error because SadDeriver hasn't added [Immutable].
	// There is a diagnostic for that mistake, but once its fixed we would
	// get a diagnostic here. It would be nicer to developers to report all
	// the violations, probably, but this keeps the implementation very simple
	// and it's unlikely to come up in practice if you make small changes
	// between compiles.
	public sealed class IndirectlySadClass : SadDeriver { }

	public partial class
	/* MissingTransitiveImmutableAttribute(Tests.PartialClass, , interface, Tests.ISomethingImmutable) */ PartialClass /**/
		: ISomethingImmutable { }

	// This one doesn't get the diagnostic. We attach it to the one that specified
	// the interface.
	public partial class PartialClass { }

	// We will emit another diagnostic here though... this makes sense but the
	// code fix will try to apply multiple [Immutable] attributes which isn't
	// allowed...
	public partial class
	/* MissingTransitiveImmutableAttribute(Tests.PartialClass, , base class, Tests.HappyImplementor) */ PartialClass /**/
		: HappyImplementor { }

	// This shouldn't crash the analyzer
	public sealed class Foo : IThingThatDoesntExist { }

	[Immutable]
	public record UnsealedImmutableRecord { }

	public sealed record
		/* MissingTransitiveImmutableAttribute(Tests.DerivedRecordMissingAttribute, , base class, Tests.UnsealedImmutableRecord) */ DerivedRecordMissingAttribute /**/
		: UnsealedImmutableRecord { }

	[Immutable]
	public sealed record SealedDerivedWithAttribute : UnsealedImmutableRecord { }

	[Immutable]
	public record UnsealedDerivedWithAttribute : UnsealedImmutableRecord { }

	public record RegularRecord { }
	public sealed RegularDerivedRecord : RegularRecord { }

	public record ConciseRecord : UnsealedImmutableRecord;

	[Immutable]
	public record BaseRecordWithArgs(int x ) {}

	[Immutable]
	public record ImmutableDerivedWithArgs(int y) : BaseRecordWithArgs(y);

	public sealed record
		/* MissingTransitiveImmutableAttribute(DerivedRecordNoAttrConstArg, , base class, BaseRecordWithArgs) */ DerivedRecordNoAttrConstArg /**/
		: BaseRecordWithArgs(0);

	public sealed record
		/* MissingTransitiveImmutableAttribute(DerivedRecordNoAttrWithArg, , base class, BaseRecordWithArgs) */ DerivedRecordNoAttrWithArg /**/
		(int z) : BaseRecordWithArgs(z);

	[ConditionallyImmutable]
	public interface ISomethingConditionallyImmutable<[ConditionallyImmutable.OnlyIf] T> { }

	[Immutable]
	public sealed class ImmutableClassImplementingConditionallyImmutable<[Immutable] T> : ISomethingConditionallyImmutable<T> { }

	[ConditionallyImmutable]
	public sealed class ConditionallyImmutableClassImplementingConditionallyImmutable<[ConditionallyImmutable.OnlyIf] T> : ISomethingConditionallyImmutable<T> { }

	public sealed class /* MissingTransitiveImmutableAttribute(SadImplementerOfConditionallyImmutable,  (or [ConditionallyImmutable]), interface, ISomethingConditionallyImmutable) */ SadImplementerOfConditionallyImmutable /**/<T> : ISomethingConditionallyImmutable<T> { }
}
