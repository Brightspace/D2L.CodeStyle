// analyzer: D2L.CodeStyle.Analyzers.Helpers.ConstantAnalyzer

using D2L.CodeStyle.Annotations;

#region Relevant Types
namespace D2L.CodeStyle.Annotations
{
	public static class Contract
	{
		public sealed class ConstantAttribute : Attribute { }
	}
}
#endregion

namespace SpecTests
{
	using static Contract;

	public sealed class Types
	{
		public static void SomeMethodWithConstantParameter([Constant] string param1) { }
		public static void SomeMethodWithParameter(string param1) { }
		public static void SomeMethodWithOneConstantParameter([Constant] string param1, string param2) { }
		public static void SomeMethodWithOneOtherConstantParameter(string param1, [Constant] string param2) { }
		public static void SomeMethodWithTwoConstantParameters([Constant] string param1, [Constant] string param2) { }
	}

	public sealed class Tests
	{
		void Method()
		{
			string variable = "This is a variable message";
			const string CONSTANT = "This is a constant message";

			Types.SomeMethodWithConstantParameter("This is a constant message");
			Types.SomeMethodWithConstantParameter(CONSTANT);
			Types.SomeMethodWithConstantParameter(CONSTANT + "This is a constant message");
			Types.SomeMethodWithConstantParameter(/* NonConstantPassedToConstantParameter(param1) */ CONSTANT + variable /**/);
			Types.SomeMethodWithConstantParameter(/* NonConstantPassedToConstantParameter(param1) */ variable /**/);

			Types.SomeMethodWithParameter("This is a constant message");
			Types.SomeMethodWithParameter(CONSTANT);
			Types.SomeMethodWithParameter(CONSTANT + "This is a constant message");
			Types.SomeMethodWithParameter(CONSTANT + variable);
			Types.SomeMethodWithParameter(variable);

			Types.SomeMethodWithOneConstantParameter("This is a constant message", "This is a constant message");
			Types.SomeMethodWithOneConstantParameter(CONSTANT, CONSTANT);
			Types.SomeMethodWithOneConstantParameter(CONSTANT + "This is a constant message", CONSTANT + "This is a constant message");
			Types.SomeMethodWithOneConstantParameter(CONSTANT, variable);
			Types.SomeMethodWithOneConstantParameter(/* NonConstantPassedToConstantParameter(param1) */ variable /**/, CONSTANT);
			Types.SomeMethodWithOneConstantParameter(/* NonConstantPassedToConstantParameter(param1) */ variable /**/, variable);

			Types.SomeMethodWithOneOtherConstantParameter("This is a constant message", "This is a constant message");
			Types.SomeMethodWithOneOtherConstantParameter(CONSTANT, CONSTANT);
			Types.SomeMethodWithOneOtherConstantParameter(CONSTANT + "This is a constant message", CONSTANT + "This is a constant message");
			Types.SomeMethodWithOneOtherConstantParameter(CONSTANT, /* NonConstantPassedToConstantParameter(param2) */ variable /**/);
			Types.SomeMethodWithOneOtherConstantParameter(variable, CONSTANT);
			Types.SomeMethodWithOneOtherConstantParameter(variable, /* NonConstantPassedToConstantParameter(param2) */ variable /**/);

			Types.SomeMethodWithTwoConstantParameters("This is a constant message", "This is a constant message");
			Types.SomeMethodWithTwoConstantParameters(CONSTANT, CONSTANT);
			Types.SomeMethodWithTwoConstantParameters(CONSTANT + "This is a constant message", CONSTANT + "This is a constant message");
			Types.SomeMethodWithTwoConstantParameters(CONSTANT, /* NonConstantPassedToConstantParameter(param2) */ variable /**/);
			Types.SomeMethodWithTwoConstantParameters(/* NonConstantPassedToConstantParameter(param1) */ variable /**/, CONSTANT);
			Types.SomeMethodWithTwoConstantParameters(/* NonConstantPassedToConstantParameter(param1) */ variable /**/, /* NonConstantPassedToConstantParameter(param2) */ variable /**/);
		}
	}
}
