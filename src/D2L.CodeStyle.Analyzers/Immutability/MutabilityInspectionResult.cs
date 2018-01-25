using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	public enum MutabilityTarget {
		Member,
		Type,
		TypeArgument
	}

	public enum MutabilityCause {
		IsADelegate,
		IsNotReadonly,
		IsNotSealed,
		IsAnInterface,
		IsAnExternalUnmarkedType,
		IsAnArray,
		IsPotentiallyMutable,
		IsDynamic,
		IsAGenericType
	}

	public sealed class MutabilityInspectionResult {

		private readonly static MutabilityInspectionResult s_notMutableResult = new MutabilityInspectionResult( false, null, null, null, null, ImmutableHashSet<string>.Empty );

		public bool IsMutable { get; }

		public string MemberPath { get; }

		public string TypeName { get; }

		public MutabilityCause? Cause { get; }

		public MutabilityTarget? Target { get; }

		public ImmutableHashSet<string> SeenUnauditedReasons { get; }

		private MutabilityInspectionResult(
			bool isMutable,
			string memberPath,
			string typeName,
			MutabilityTarget? target,
			MutabilityCause? cause,
			ImmutableHashSet<string> seenUnauditedReasons
		) {
			IsMutable = isMutable;
			MemberPath = memberPath;
			TypeName = typeName;
			Target = target;
			Cause = cause;
			SeenUnauditedReasons = seenUnauditedReasons;
		}

		public static MutabilityInspectionResult NotMutable() {
			return s_notMutableResult;
		}

		public static MutabilityInspectionResult NotMutable( ImmutableHashSet<string> seenUnauditedReasons ) {
			return new MutabilityInspectionResult(
					false,
					null,
					null,
					null,
					null,
					seenUnauditedReasons
				);
		}

		public static MutabilityInspectionResult Mutable(
			string mutableMemberPath,
			string membersTypeName,
			MutabilityTarget kind,
			MutabilityCause cause
		) {
			return new MutabilityInspectionResult(
				true,
				mutableMemberPath,
				membersTypeName,
				kind,
				cause,
				ImmutableHashSet<string>.Empty
			);
		}

		public static MutabilityInspectionResult MutableType(
			ITypeSymbol type,
			MutabilityCause cause
		) {
			return Mutable(
				null,
				type.GetFullTypeName(),
				MutabilityTarget.Type,
				cause
			);
		}

		public static MutabilityInspectionResult MutableField(
			IFieldSymbol field,
			MutabilityCause cause
		) {
			return Mutable(
				field.Name,
				field.Type.GetFullTypeName(),
				MutabilityTarget.Member,
				cause
			);
		}

		public static MutabilityInspectionResult MutableProperty(
			IPropertySymbol property,
			MutabilityCause cause
		) {
			return Mutable(
				property.Name,
				property.Type.GetFullTypeName(),
				MutabilityTarget.Member,
				cause
			);
		}

		public static MutabilityInspectionResult PotentiallyMutableMember(
			ISymbol member
		) {
			return Mutable(
				member.Name,
				null,
				MutabilityTarget.Member,
				MutabilityCause.IsPotentiallyMutable
			);
		}

		public MutabilityInspectionResult WithPrefixedMember( string parentMember ) {
			if ( !IsMutable ) {
				return this; // minor optimization
			}

			var newMember = string.IsNullOrWhiteSpace( this.MemberPath )
				? parentMember
				: $"{parentMember}.{this.MemberPath}";

			return new MutabilityInspectionResult(
				this.IsMutable,
				newMember,
				this.TypeName,
				this.Target,
				this.Cause,
				this.SeenUnauditedReasons
			);
		}

		public MutabilityInspectionResult WithTarget( MutabilityTarget newTarget ) {
			return new MutabilityInspectionResult(
				this.IsMutable,
				this.MemberPath,
				this.TypeName,
				newTarget,
				this.Cause,
				this.SeenUnauditedReasons
			);
		}

	}
}
