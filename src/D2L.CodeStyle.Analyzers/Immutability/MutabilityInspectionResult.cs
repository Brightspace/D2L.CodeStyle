using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

		private readonly static MutabilityInspectionResult s_notMutableResult = new MutabilityInspectionResult(
			false,
			null,
			null,
			null,
			null,
			ImmutableHashSet<string>.Empty,
			ImmutableHashSet<AttributeSyntax>.Empty
		);

		public bool IsMutable { get; }

		public string MemberPath { get; }

		public string TypeName { get; }

		public MutabilityCause? Cause { get; }

		public MutabilityTarget? Target { get; }

		public ImmutableHashSet<string> SeenUnauditedReasons { get; }

		public ImmutableHashSet<AttributeSyntax> UnnecessaryAnnotations { get; }

		private MutabilityInspectionResult(
			bool isMutable,
			string memberPath,
			string typeName,
			MutabilityTarget? target,
			MutabilityCause? cause,
			ImmutableHashSet<string> seenUnauditedReasons,
			ImmutableHashSet<AttributeSyntax> unnecessaryAnnotations
		) {
			IsMutable = isMutable;
			MemberPath = memberPath;
			TypeName = typeName;
			Target = target;
			Cause = cause;
			SeenUnauditedReasons = seenUnauditedReasons;
			UnnecessaryAnnotations = unnecessaryAnnotations;
		}

		public static MutabilityInspectionResult NotMutable() {
			return s_notMutableResult;
		}

		public static MutabilityInspectionResult Annotated(
			MutabilityInspectionResult inspectionResult,
			IEnumerable<AttributeSyntax> attributeSyntaxes
		) {
			if( inspectionResult.IsMutable ) {
				return s_notMutableResult;
			}

			return s_notMutableResult
					.WithUnnecessaryAnnotations(
						inspectionResult.UnnecessaryAnnotations.Union( attributeSyntaxes )
					);
		}

		public static MutabilityInspectionResult NotMutable( ImmutableHashSet<string> seenUnauditedReasons ) {
			return new MutabilityInspectionResult(
					false,
					null,
					null,
					null,
					null,
					seenUnauditedReasons,
					ImmutableHashSet<AttributeSyntax>.Empty
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
				ImmutableHashSet<string>.Empty,
				ImmutableHashSet<AttributeSyntax>.Empty
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
				this.SeenUnauditedReasons,
				this.UnnecessaryAnnotations
			);
		}

		public MutabilityInspectionResult WithTarget( MutabilityTarget newTarget ) {
			return new MutabilityInspectionResult(
				this.IsMutable,
				this.MemberPath,
				this.TypeName,
				newTarget,
				this.Cause,
				this.SeenUnauditedReasons,
				this.UnnecessaryAnnotations
			);
		}

		public MutabilityInspectionResult WithSeenUnauditedReason(
			string unnecessaryAnnotation
		) {
			return new MutabilityInspectionResult(
				this.IsMutable,
				this.MemberPath,
				this.TypeName,
				this.Target,
				this.Cause,
				this.SeenUnauditedReasons.Add( unnecessaryAnnotation ),
				this.UnnecessaryAnnotations
			);
		}

		public MutabilityInspectionResult WithUnnecessaryAnnotations(
			ImmutableHashSet<AttributeSyntax> unnecessaryAnnotations
		) {
			return new MutabilityInspectionResult(
				this.IsMutable,
				this.MemberPath,
				this.TypeName,
				this.Target,
				this.Cause,
				this.SeenUnauditedReasons,
				this.UnnecessaryAnnotations.Union( unnecessaryAnnotations )
			);
		}

	}
}
