// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.ConsistentParameterAttributesAnalyzer, D2L.CodeStyle.Analyzers

using System;
using D2L.CodeStyle.Annotations.Contract

namespace SpecTests {

	interface SomeInterface {
		void EmptyMethod();
		void MethodA( string foo, Func<string> bar );
		void MethodB( [Constant] string foo, [StatelessFunc] Func<string> bar );
	}

	class SomeBaseClass {
		public abstract void EmptyMethod();
		public abstract void MethodA( string foo, Func<string> bar );
		public abstract void MethodB( [Constant] string foo, [StatelessFunc] Func<string> bar );
	}

	class ImplicitClassOkay : SomeBaseClass, SomeInterface {

		public override void EmptyMethod() => throw new NotImplementedException();
		public override void MethodA( string foo, Func<string> bar ) => throw new NotImplementedException();
		public override void MethodB( [Constant] string foo, [StatelessFunc] Func<string> bar ) => throw new NotImplementedException();

	}

	class ExplicitClassOkay : SomeInterface {

		void SomeInterface.EmptyMethod() => throw new NotImplementedException();
		void SomeInterface.MethodA( string foo, Func<string> bar ) => throw new NotImplementedException();
		void SomeInterface.MethodB( [Constant] string foo, [StatelessFunc] Func<string> bar ) => throw new NotImplementedException();

	}

	class ImplicitClassBad : SomeBaseClass, SomeInterface {

		public override void EmptyMethod() => throw new NotImplementedException();
		public override void MethodA(
			/* InconsistentMethodAttributeApplication(Constant, ImplicitClassBad.MethodA, SomeBaseClass.MethodA) | InconsistentMethodAttributeApplication(Constant, ImplicitClassBad.MethodA, SomeInterface.MethodA) */ [Constant] string foo /**/,
			/* InconsistentMethodAttributeApplication(StatelessFunc, ImplicitClassBad.MethodA, SomeBaseClass.MethodA) | InconsistentMethodAttributeApplication(StatelessFunc, ImplicitClassBad.MethodA, SomeInterface.MethodA) */ [StatelessFunc] Func<string> bar /**/
		) => throw new NotImplementedException();
		public override void MethodB(
			/* InconsistentMethodAttributeApplication(Constant, ImplicitClassBad.MethodB, SomeBaseClass.MethodB) | InconsistentMethodAttributeApplication(Constant, ImplicitClassBad.MethodB, SomeInterface.MethodB) */ string foo /**/,
			/* InconsistentMethodAttributeApplication(StatelessFunc, ImplicitClassBad.MethodB, SomeBaseClass.MethodB) | InconsistentMethodAttributeApplication(StatelessFunc, ImplicitClassBad.MethodB, SomeInterface.MethodB) */ Func<string> bar /**/
		) => throw new NotImplementedException();

	}

	class ExplicitClassBad : SomeInterface {

		void SomeInterface.EmptyMethod() => throw new NotImplementedException();
		void SomeInterface.MethodA(
			/* InconsistentMethodAttributeApplication(Constant, ExplicitClassBad.SpecTests.SomeInterface.MethodA, SomeInterface.MethodA) */ [Constant] string foo /**/,
			/* InconsistentMethodAttributeApplication(StatelessFunc, ExplicitClassBad.SpecTests.SomeInterface.MethodA, SomeInterface.MethodA) */ [StatelessFunc] Func<string> bar /**/
		) => throw new NotImplementedException();
		void SomeInterface.MethodB(
			/* InconsistentMethodAttributeApplication(Constant, ExplicitClassBad.SpecTests.SomeInterface.MethodB, SomeInterface.MethodB) */ string foo /**/,
			/* InconsistentMethodAttributeApplication(StatelessFunc, ExplicitClassBad.SpecTests.SomeInterface.MethodB, SomeInterface.MethodB) */ Func<string> bar /**/
		) => throw new NotImplementedException();

	}

}
