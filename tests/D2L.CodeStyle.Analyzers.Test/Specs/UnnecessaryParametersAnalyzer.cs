// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.UnnecessaryParameters.UnnecessaryParametersAnalyzer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2L.Core.Security;

namespace D2L.Core.Security {

	public static class D2LSecurity {

		public abstract bool HasPermission( string permName, long roleId, long ouTypeId, bool isAuditor, int auditorRelationshipTypeId );
		public abstract bool HasPermission( long toolVarId, long roleId, long ouTypeId, bool isAuditor, int auditorRelationshipTypeId );
		public abstract bool HasPermission( string permName, long roleId, long ouTypeId );
		public abstract bool HasPermission( long toolVarId, long roleId, long ouTypeId );

		public abstract bool HasCapability( long capabilityId, long allowedRoleId, long roleId, long orgUnitTypeId, bool isAuditor, int auditorRelationshipTypeId );
		public abstract bool HasCapability( string capabilityName, long allowedRoleId, long roleId, long orgUnitTypeId, bool isAuditor, int auditorRelationshipTypeId );
		public abstract bool HasCapability( long capabilityId, long allowedRoleId, long roleId, long orgUnitTypeId );
		public abstract bool HasCapability( string capabilityName, long allowedRoleId, long roleId, long orgUnitTypeId );
	}
}

namespace D2L.CodeStyle.Analyzers.Tests.Specs {

	public static class UnnecessaryParametersAnalyzer {
		public void/* ParametersShouldBeRemoved(D2L.Core.Security.D2LSecurity.HasPermission) */ MethodHasPermission(/**/) {

			D2LSecurity d2lSecurity = new D2LSecurity();
			d2lSecurity.HasPermission("", 0, 0, false, 0);
			d2lSecurity.HasPermission(0, 0, 0, false, 0);
		}

		public void/* ParametersShouldBeRemoved(D2L.Core.Security.D2LSecurity.HasCapability) */ MethodHasCapability(/**/) {

			D2LSecurity d2lSecurity = new D2LSecurity();
			d2lSecurity.HasCapability("", 0, 0, 0, false, 0);
			d2lSecurity.HasCapability(0, 0, 0, 0, false, 0);
		}

		public void MethodWithHasPermissionNoUnnecessaryParam() {

			D2LSecurity d2lSecurity = new D2LSecurity();
			d2lSecurity.HasPermission("", 0, 0);
			d2lSecurity.HasPermission(0, 0, 0);
		}
	}
}
