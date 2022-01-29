// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DangerousMemberUsages.DangerousMemberUsagesAnalyzer

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

namespace System.Web.Hosting {

	public static class HostingEnvironment {

		public static string MapPath( string virtualPath ) => virtualPath + " mapped";

	}
}

namespace System.Web {

	public class HttpServerUtility {

		public void Transfer( string path ) {}
	}

}

namespace SpecTests {

	internal sealed class UnmarkedUsages {

		public /* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ UnmarkedUsages(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ Method(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Threading.Tasks.Task.Run) */ AsyncMethod(/**/) {
			Task.Run<int>( () => Task.FromResult( 7 ) );
		}

		public int PropertyGetter {
		/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */	get /**/{
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
				return 1;
			}
		}

		public int PropertySetter {
		/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */	set /**/{
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", value, null );
			}
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ DelegateInsideMethod(/**/) {

			Action hacker = () => {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
			};
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Web.Hosting.HostingEnvironment.MapPath) */ MethodWithMapPath(/**/) {
			System.Web.Hosting.HostingEnvironment.MapPath( "/d2l" );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Web.HttpServerUtility.Transfer) */ MethodWithTransfer(/**/) {
			HttpServerUtility obj = new HttpServerUtility();
			obj.Transfer( "/new/path" );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Threading.Tasks.TaskFactory.StartNew) */ MethodWithTaskFactoryStartNew(/**/) {
			System.Threading.Tasks.Task.Factory.StartNew( () => { } );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Threading.Tasks.TaskFactory.StartNew) */ MethodWithGenericTaskFactoryStartNew(/**/) {
			System.Threading.Tasks.Task<int>.Factory.StartNew( () => 1 );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Threading.Thread.Sleep) */ MethodWithThreadSleepInt(/**/) {
			System.Threading.Thread.Sleep( 1 );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Threading.Thread.Sleep) */ MethodWithThreadSleepTimeSpan(/**/) {
			System.Threading.Thread.Sleep( TimeSpan.FromMilliseconds( 1 ) );
		}

		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ MethodReference(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			Action<object, object, object[]> setter = p.SetValue;
		}
	}

	internal sealed class AuditedUsages {

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue", "John Doe", "1970-01-01", "Rationale" )]
		public AuditedUsages() {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue", "John Doe", "1970-01-01", "Rationale" )]
		public void Method() {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited( typeof( Task ), "Run", "John Doe", "1970-01-01", "Rationale" )]
		public void AsyncMethod() {
			Task.Run<int>( () => Task.FromResult( 7 ) );
		}

		public int PropertyGetter {

			[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue", "John Doe", "1970-01-01", "Rationale" )]
			get {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
				return 1;
			}
		}

		public int PropertySetter {

			[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue", "John Doe", "1970-01-01", "Rationale" )]
			set {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", value, null );
			}
		}

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue", "John Doe", "1970-01-01", "Rationale" )]
		public void DelegateInsideMethod() {

			Action hacker = () => {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
			};
		}

		[DangerousMethodUsage.Audited( typeof( HostingEnvironment ), "MapPath", "John Doe", "1970-01-01", "Rationale" )]
		public void MethodWithMapPath() {
			HostingEnvironment.MapPath( "/d2l" );
		}

		[DangerousMethodUsage.Audited( typeof( HttpServerUtility ), "Transfer", "John Doe", "1970-01-01", "Rationale" )]
		public void MethodWithTransfer() {
			HttpServerUtility obj = new HttpServerUtility();
			obj.Transfer( "/new/path" );
		}
	}

	internal sealed class UnauditedUsages {

		[DangerousMethodUsage.Unaudited( typeof( PropertyInfo ), "SetValue" )]
		public UnauditedUsages() {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Unaudited( typeof( PropertyInfo ), "SetValue" )]
		public void Method() {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Unaudited( typeof( Task ), "Run" )]
		public void AsyncMethod() {
			Task.Run<int>( () => Task.FromResult( 7 ) );
		}

		public int PropertyGetter {

			[DangerousMethodUsage.Unaudited( typeof( PropertyInfo ), "SetValue" )]
			get {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
				return 1;
			}
		}

		public int PropertySetter {

			[DangerousMethodUsage.Unaudited( typeof( PropertyInfo ), "SetValue" )]
			set {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", value, null );
			}
		}

		[DangerousMethodUsage.Unaudited( typeof( PropertyInfo ), "SetValue" )]
		public void DelegateInsideMethod() {

			Action hacker = () => {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
			};
		}

		[DangerousMethodUsage.Unaudited( typeof( HostingEnvironment ), "MapPath" )]
		public void MethodWithMapPath() {
			HostingEnvironment.MapPath( "/d2l" );
		}

		[DangerousMethodUsage.Unaudited( typeof( HttpServerUtility ), "Transfer" )]
		public void MethodWithTransfer() {
			HttpServerUtility obj = new HttpServerUtility();
			obj.Transfer( "/new/path" );
		}
	}

	internal sealed class MismatchedAuditedUsages {

		[DangerousMethodUsage.Audited( null, "SetValue" )]
		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ NullDeclaringType(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited( typeof( FieldInfo ), "SetValue" )]
		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ DifferentDeclaringType(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), null )]
		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ NullMethodName(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "GetValue" )]
		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ DifferentMethodName(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited]
		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ MissingParameters(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Unaudited( null, "MapPath" )]
		public void/* DangerousMethodsShouldBeAvoided(System.Web.Hosting.HostingEnvironment.MapPath) */ MethodWithMapPath(/**/) {
			HostingEnvironment.MapPath( "/d2l" );
		}

		[DangerousMethodUsage.Unaudited( null, "Transfer" )]
		public void/* DangerousMethodsShouldBeAvoided(System.Web.HttpServerUtility.Transfer) */ MethodWithTransfer(/**/) {
			System.Web.HttpServerUtility obj = new System.Web.HttpServerUtility();
			obj.Transfer( "/new/path" );
		}
	}

	internal sealed class UnrelatedAttributes {

		[Objects.Immutable]
		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ Method(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}
	}
}
