// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages.DangerousMemberUsagesAnalyzer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecTests{
	public class SketchyClass {
		[D2L.CodeStyle.Annotations.Objects.DangerousProperty]
		public int DangerousProperty { get; }

		[D2L.CodeStyle.Annotations.Objects.DangerousProperty]
		public int PropertyDefinedButNotUsed { get; }

		public int OkayProperty { get; }

		[D2L.CodeStyle.Annotations.Objects.DangerousMethod]
		public void DangerousMethod();

		public void OkayMethod();
	}

	public class 🦝 {
		public void/* DangerousMethodsShouldBeAvoided(SpecTests.SketchyClass.DangerousMethod) */ MethodCallingDangerousMethod(/**/SketchyClass w) {
			w.DangerousMethod();
		}

		public void MethodCallingOkayMethod( SketchyClass w ) {
			w.OkayMethod();
        }

		public int/* DangerousPropertiesShouldBeAvoided(SpecTests.SketchyClass.DangerousProperty) */ MethodCallingDangerousProperty(/**/ SketchyClass w ) {
			return w.DangerousProperty;
        }

		[D2L.CodeStyle.Annotations.DangerousMethodUsage.Audited( declaringType:typeof(SketchyClass), methodName:"DangerousMethod", owner:"me", auditedDate:"today", rationale: "wow" )]
		public void AuditedMethodCallingDangerousMethod( SketchyClass w ) {
			w.DangerousMethod();
		}

		[D2L.CodeStyle.Annotations.DangerousMethodUsage.Unaudited( declaringType: typeof(SketchyClass), methodName: "DangerousMethod" )]
		public void UnauditedMethodCallingDangerousMethod( SketchyClass w ) {
			w.DangerousMethod();
		}

		[D2L.CodeStyle.Annotations.DangerousPropertyUsage.Audited( declaringType:typeof(SketchyClass), propertyName: "DangerousProperty", owner:"me", auditedDate:"today", rationale: "wow" )]
		public int AuditedMethodCallingDangerousProperty( SketchyClass w ) {
			return w.DangerousProperty;
		}

		[D2L.CodeStyle.Annotations.DangerousPropertyUsage.Unaudited( declaringType:typeof(SketchyClass), propertyName: "DangerousProperty" )]
		public int UnauditedMethodCallingDangerousProperty( SketchyClass w ) {
			return w.DangerousProperty;
		}
	}
}
