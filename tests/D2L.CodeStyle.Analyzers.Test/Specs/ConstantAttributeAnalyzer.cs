// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.ConstantAttributeAnalyzer

using System;

namespace SpecTests
{
	using D2L.CodeStyle.Annotations.Contract;

	public sealed class Logger {
		public static void Error( [Constant] string message ) { }
	}

	public sealed class WrappedLogger {
		public static void Error( [Constant] string message ) {
			Logger.Error( message );
		}
		public static void OtherError( string message ) {
			Logger.Error( /* NonConstantPassedToConstantParameter(message) */ message /**/ );
		}
	}

	public sealed class Types
	{
		public static void SomeMethodWithConstantParameter<T>( [Constant] T param1 ) { }
		public static void SomeMethodWithParameter<T>( T param1 ) { }
		public static void SomeMethodWithOneConstantParameter<T>( [Constant] T param1, T param2 ) { }
		public static void SomeMethodWithOneOtherConstantParameter<T>( T param1, [Constant] T param2 ) { }
		public static void SomeMethodWithTwoConstantParameters<T>( [Constant] T param1, [Constant] T param2 ) { }

		public interface IInterface { }
		public class SomeClassImplementingInterface : IInterface { }
		public static void SomeMethodWithInterfaceParameter( /* InvalidConstantType(Interface) */ [Constant] IInterface @interface /**/ ) { }
		public static void SomeMethodWithOneInterfaceParameter<T>(IInterface param1, [Constant] T param2) { }
	}

	public sealed class Tests
	{
		void Method()
		{
			#region Invalid type tests
			const Types.SomeClassImplementingInterface interfaceClass = new Types.SomeClassImplementingInterface { };
			Types.SomeMethodWithConstantParameter<Types.IInterface>( /* NonConstantPassedToConstantParameter(param1) */ interfaceClass /**/ );
			Types.SomeMethodWithOneInterfaceParameter<Types.IInterface>( interfaceClass, /* NonConstantPassedToConstantParameter(param2) */ interfaceClass /**/ );
			#endregion

			#region Logger tests
			const string CONSTANT_MESSAGE = "Organization {0} is not constant.";
			int orgId = 0;

			Logger.Error( CONSTANT_MESSAGE );
			Logger.Error( "Organization 1 is constant." );
			Logger.Error( /* NonConstantPassedToConstantParameter(message) */ string.Format(CONSTANT_MESSAGE, orgId) /**/ );
			Logger.Error( /* NonConstantPassedToConstantParameter(message) */ $"Organization {orgId} is not constant." /**/ );

			const string CONSTANT_STRING = "Foo";
			Logger.Error( $"Interpolated strings of constant strings are compile-time constants as of C#10." );
			#endregion

			#region String tests
			string variableStr = "This is a variable message";
			const string CONSTANT_STR = "This is a constant message";

			Types.SomeMethodWithConstantParameter<string>( "This is a constant message" );
			Types.SomeMethodWithConstantParameter<string>( CONSTANT_STR );
			Types.SomeMethodWithConstantParameter<string>( CONSTANT_STR + "This is a constant message" );
			Types.SomeMethodWithConstantParameter<string>( /* NonConstantPassedToConstantParameter(param1) */ CONSTANT_STR + variableStr /**/ );
			Types.SomeMethodWithConstantParameter<string>( /* NonConstantPassedToConstantParameter(param1) */ variableStr /**/ );

			Types.SomeMethodWithParameter<string>( "This is a constant message" );
			Types.SomeMethodWithParameter<string>( CONSTANT_STR );
			Types.SomeMethodWithParameter<string>( CONSTANT_STR + "This is a constant message" );
			Types.SomeMethodWithParameter<string>( CONSTANT_STR + variableStr );
			Types.SomeMethodWithParameter<string>( variableStr );

			Types.SomeMethodWithOneConstantParameter<string>( "This is a constant message", "This is a constant message" );
			Types.SomeMethodWithOneConstantParameter<string>( CONSTANT_STR, CONSTANT_STR );
			Types.SomeMethodWithOneConstantParameter<string>( CONSTANT_STR + "This is a constant message", CONSTANT_STR + "This is a constant message" );
			Types.SomeMethodWithOneConstantParameter<string>( CONSTANT_STR, variableStr );
			Types.SomeMethodWithOneConstantParameter<string>( /* NonConstantPassedToConstantParameter(param1) */ variableStr /**/, CONSTANT_STR );
			Types.SomeMethodWithOneConstantParameter<string>( /* NonConstantPassedToConstantParameter(param1) */ variableStr /**/, variableStr );

			Types.SomeMethodWithOneOtherConstantParameter<string>( "This is a constant message", "This is a constant message" );
			Types.SomeMethodWithOneOtherConstantParameter<string>( CONSTANT_STR, CONSTANT_STR );
			Types.SomeMethodWithOneOtherConstantParameter<string>( CONSTANT_STR + "This is a constant message", CONSTANT_STR + "This is a constant message" );
			Types.SomeMethodWithOneOtherConstantParameter<string>( CONSTANT_STR, /* NonConstantPassedToConstantParameter(param2) */ variableStr /**/ );
			Types.SomeMethodWithOneOtherConstantParameter<string>( variableStr, CONSTANT_STR );
			Types.SomeMethodWithOneOtherConstantParameter<string>( variableStr, /* NonConstantPassedToConstantParameter(param2) */ variableStr /**/ );

			Types.SomeMethodWithTwoConstantParameters<string>( "This is a constant message", "This is a constant message" );
			Types.SomeMethodWithTwoConstantParameters<string>( CONSTANT_STR, CONSTANT_STR );
			Types.SomeMethodWithTwoConstantParameters<string>( CONSTANT_STR + "This is a constant message", CONSTANT_STR + "This is a constant message" );
			Types.SomeMethodWithTwoConstantParameters<string>( CONSTANT_STR, /* NonConstantPassedToConstantParameter(param2) */ variableStr /**/ );
			Types.SomeMethodWithTwoConstantParameters<string>( /* NonConstantPassedToConstantParameter(param1) */ variableStr /**/, CONSTANT_STR );
			Types.SomeMethodWithTwoConstantParameters<string>( /* NonConstantPassedToConstantParameter(param1) */ variableStr /**/, /* NonConstantPassedToConstantParameter(param2) */ variableStr /**/ );
			#endregion

			#region Number tests
			int variableInt = 5;
			const int CONSTANT_INT = 29;

			Types.SomeMethodWithConstantParameter<int>( 29 );
			Types.SomeMethodWithConstantParameter<int>( CONSTANT_INT );
			Types.SomeMethodWithConstantParameter<int>( CONSTANT_INT + 29 );
			Types.SomeMethodWithConstantParameter<int>( /* NonConstantPassedToConstantParameter(param1) */ CONSTANT_INT + variableInt /**/ );
			Types.SomeMethodWithConstantParameter<int>( /* NonConstantPassedToConstantParameter(param1) */ variableInt /**/ );

			Types.SomeMethodWithParameter<int>( 29 );
			Types.SomeMethodWithParameter<int>( CONSTANT_INT );
			Types.SomeMethodWithParameter<int>( CONSTANT_INT + 29 );
			Types.SomeMethodWithParameter<int>( CONSTANT_INT + variableInt );
			Types.SomeMethodWithParameter<int>( variableInt );

			Types.SomeMethodWithOneConstantParameter<int>( 29, 29 );
			Types.SomeMethodWithOneConstantParameter<int>( CONSTANT_INT, CONSTANT_INT );
			Types.SomeMethodWithOneConstantParameter<int>( CONSTANT_INT + 29, CONSTANT_INT + 29 );
			Types.SomeMethodWithOneConstantParameter<int>( CONSTANT_INT, variableInt );
			Types.SomeMethodWithOneConstantParameter<int>( /* NonConstantPassedToConstantParameter(param1) */ variableInt /**/, CONSTANT_INT );
			Types.SomeMethodWithOneConstantParameter<int>( /* NonConstantPassedToConstantParameter(param1) */ variableInt /**/, variableInt );

			Types.SomeMethodWithOneOtherConstantParameter<int>( 29, 29 );
			Types.SomeMethodWithOneOtherConstantParameter<int>( CONSTANT_INT, CONSTANT_INT );
			Types.SomeMethodWithOneOtherConstantParameter<int>( CONSTANT_INT + 29, CONSTANT_INT + 29 );
			Types.SomeMethodWithOneOtherConstantParameter<int>( CONSTANT_INT, /* NonConstantPassedToConstantParameter(param2) */ variableInt /**/ );
			Types.SomeMethodWithOneOtherConstantParameter<int>( variableInt, CONSTANT_INT );
			Types.SomeMethodWithOneOtherConstantParameter<int>( variableInt, /* NonConstantPassedToConstantParameter(param2) */ variableInt /**/ );

			Types.SomeMethodWithTwoConstantParameters<int>( 29, 29 );
			Types.SomeMethodWithTwoConstantParameters<int>( CONSTANT_INT, CONSTANT_INT );
			Types.SomeMethodWithTwoConstantParameters<int>( CONSTANT_INT + 29, CONSTANT_INT + 29 );
			Types.SomeMethodWithTwoConstantParameters<int>( CONSTANT_INT, /* NonConstantPassedToConstantParameter(param2) */ variableInt /**/ );
			Types.SomeMethodWithTwoConstantParameters<int>( /* NonConstantPassedToConstantParameter(param1) */ variableInt /**/, CONSTANT_INT );
			Types.SomeMethodWithTwoConstantParameters<int>( /* NonConstantPassedToConstantParameter(param1) */ variableInt /**/, /* NonConstantPassedToConstantParameter(param2) */ variableInt /**/ );
			#endregion

			#region Boolean tests
			bool variableBool = true;
			const bool CONSTANT_BOOL = false;

			Types.SomeMethodWithConstantParameter<bool>( false );
			Types.SomeMethodWithConstantParameter<bool>( CONSTANT_BOOL );
			Types.SomeMethodWithConstantParameter<bool>( CONSTANT_BOOL || false );
			Types.SomeMethodWithConstantParameter<bool>( /* NonConstantPassedToConstantParameter(param1) */ CONSTANT_BOOL || variableBool /**/ );
			Types.SomeMethodWithConstantParameter<bool>( /* NonConstantPassedToConstantParameter(param1) */ variableBool /**/ );

			Types.SomeMethodWithParameter<bool>( false );
			Types.SomeMethodWithParameter<bool>( CONSTANT_BOOL );
			Types.SomeMethodWithParameter<bool>( CONSTANT_BOOL || false );
			Types.SomeMethodWithParameter<bool>( CONSTANT_BOOL || variableBool );
			Types.SomeMethodWithParameter<bool>( variableBool );

			Types.SomeMethodWithOneConstantParameter<bool>( false, false );
			Types.SomeMethodWithOneConstantParameter<bool>( CONSTANT_BOOL, CONSTANT_BOOL );
			Types.SomeMethodWithOneConstantParameter<bool>( CONSTANT_BOOL || false, CONSTANT_BOOL || false );
			Types.SomeMethodWithOneConstantParameter<bool>( CONSTANT_BOOL, variableBool );
			Types.SomeMethodWithOneConstantParameter<bool>( /* NonConstantPassedToConstantParameter(param1) */ variableBool /**/, CONSTANT_BOOL );
			Types.SomeMethodWithOneConstantParameter<bool>( /* NonConstantPassedToConstantParameter(param1) */ variableBool /**/, variableBool );

			Types.SomeMethodWithOneOtherConstantParameter<bool>( false, false );
			Types.SomeMethodWithOneOtherConstantParameter<bool>( CONSTANT_BOOL, CONSTANT_BOOL );
			Types.SomeMethodWithOneOtherConstantParameter<bool>( CONSTANT_BOOL || false, CONSTANT_BOOL || false );
			Types.SomeMethodWithOneOtherConstantParameter<bool>( CONSTANT_BOOL, /* NonConstantPassedToConstantParameter(param2) */ variableBool /**/ );
			Types.SomeMethodWithOneOtherConstantParameter<bool>( variableBool, CONSTANT_BOOL );
			Types.SomeMethodWithOneOtherConstantParameter<bool>( variableBool, /* NonConstantPassedToConstantParameter(param2) */ variableBool /**/ );

			Types.SomeMethodWithTwoConstantParameters<bool>( false, false );
			Types.SomeMethodWithTwoConstantParameters<bool>( CONSTANT_BOOL, CONSTANT_BOOL );
			Types.SomeMethodWithTwoConstantParameters<bool>( CONSTANT_BOOL || false, CONSTANT_BOOL || false );
			Types.SomeMethodWithTwoConstantParameters<bool>( CONSTANT_BOOL, /* NonConstantPassedToConstantParameter(param2) */ variableBool /**/ );
			Types.SomeMethodWithTwoConstantParameters<bool>( /* NonConstantPassedToConstantParameter(param1) */ variableBool /**/, CONSTANT_BOOL );
			Types.SomeMethodWithTwoConstantParameters<bool>( /* NonConstantPassedToConstantParameter(param1) */ variableBool /**/, /* NonConstantPassedToConstantParameter(param2) */ variableBool /**/ );
			#endregion
		}
	}
}
