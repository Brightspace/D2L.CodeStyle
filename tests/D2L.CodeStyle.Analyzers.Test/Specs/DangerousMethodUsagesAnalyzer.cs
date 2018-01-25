// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DangerousMethodUsages.DangerousMethodUsagesAnalyzer

using System;
using System.Reflection;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;
using D2L.LP.Extensibility.Activation.Domain;

namespace D2L.CodeStyle.Annotations {
	public static class DangerousMethodUsage {

		public sealed class AuditedAttribute : Attribute {

			public AuditedAttribute( Type declaringType, string methodName ) { }
		}

		public sealed class UnauditedAttribute : Attribute {

			public UnauditedAttribute( Type declaringType, string methodName ) { }
		}
	}

	public sealed class ImmutableAttribute : Attribute { }
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
	}

	internal sealed class AuditedUsages {

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue" )]
		public AuditedUsages() {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue" )]
		public void Method() {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}

		[DangerousMethodUsage.Audited( typeof( Task ), "Run" )]
		public void AsyncMethod() {
			Task.Run<int>( () => Task.FromResult( 7 ) );
		}

		public int PropertyGetter {

			[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue" )]
			get {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
				return 1;
			}
		}

		public int PropertySetter {

			[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue" )]
			set {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", value, null );
			}
		}

		[DangerousMethodUsage.Audited( typeof( PropertyInfo ), "SetValue" )]
		public void DelegateInsideMethod() {

			Action hacker = () => {
				PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
				p.SetValue( "str", 7, null );
			};
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
	}

	internal sealed class UnrelatedAttributes {

		[ImmutableAttribute]
		public void/* DangerousMethodsShouldBeAvoided(System.Reflection.PropertyInfo.SetValue) */ Method(/**/) {
			PropertyInfo p = typeof( string ).GetProperty( nameof( string.Length ) );
			p.SetValue( "str", 7, null );
		}
	}
}