// analyzer: D2L.CodeStyle.Analyzers.Async.BlockingAnalyzer

using System;
using System.Threading.Tasks;
using D2L.CodeStyle.Annotations;

namespace D2L.CodeStyle.Annotations {
	[AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = true )]
	public sealed class BlockingAttribute : Attribute { }
}

namespace D2L.CodeStyle.Analyzers.Async {
	public sealed class BasicTests {
		// Ok things:

		public static void VanillaFunction() {}

		public static async Task SomethingAsync() { await Task.Delay(1); }

		[/* UnnecessaryBlocking(SomeBlockingMethod) */ Blocking /**/]
		public static void SomeBlockingMethod() { }

		[Blocking]
		public static int AnotherBlockingMethod() {
			SomeBlockingMethod();
			return 3;
		}

		// Broken things:

		public async Task AccidentallyCalledBlockingInAsync() {
			/* AsyncMethodCannotCallBlockingMethod(SomeBlockingMethod) */ SomeBlockingMethod() /**/;
			await SomethingAsync();
		}

		[/* AsyncMethodCannotBeBlocking(BlockingAndAsync) */ Blocking /**/]
		public async Task BlockingAndAsync() {
			await SomethingAsync();
		}

		public void ForgotBlocking() {
			/* BlockingCallersMustBeBlocking(SomeBlockingMethod,ForgotBlocking) */ SomeBlockingMethod() /**/;
		}

		// Blocking calls in initializers are surprising/dangerous
		private readonly int m_someInt = /* OnlyCallBlockingMethodsFromMethods(AnotherBlockingMethod, field initializers) */ AnotherBlockingMethod() /**/;

		// Blocking calls in properties are surprising/dangerous
		public int SomeInt => /* OnlyCallBlockingMethodsFromMethods(AnotherBlockingMethod, properties) */ AnotherBlockingMethod() /**/;
		public int SomeInt2 {
			get {
				return /* OnlyCallBlockingMethodsFromMethods(AnotherBlockingMethod, properties) */ AnotherBlockingMethod() /**/;
			}
		}

		// Blocking calls in constructors are surprising/dangerous
		public BlockingAnalyzerTests() {
			/* OnlyCallBlockingMethodsFromMethods(SomeBlockingMethod, constructors) */ SomeBlockingMethod() /**/;
		}

		delegate void Del();

		public void NonBlockingMethodWithBlockingSubMethod() {
			[Blocking]
			void BlockingSubMethod() {
				BasicTests.SomeBlockingMethod();
			}

			/* BlockingCallersMustBeBlocking(BlockingSubMethod,NonBlockingMethodWithBlockingSubMethod) */ BlockingSubMethod() /**/;

			var x = _ =>
				/* OnlyCallBlockingMethodsFromMethods(SomeBlockingMethod,lambdas) */ BasicTests.SomeBlockingMethod() /**/;

			// The other lambda syntax with parentisized args:
			var y = (w, z) =>
				/* OnlyCallBlockingMethodsFromMethods(SomeBlockingMethod,lambdas) */ BasicTests.SomeBlockingMethod() /**/;

			Del del3 = delegate () { /* OnlyCallBlockingMethodsFromMethods(SomeBlockingMethod,delegates) */ BasicTests.SomeBlockingMethod() /**/; };
        }
	}

	public static class InheritanceTests {
		public interface IInterface {
			[Blocking]
			void BlockingInterfaceMethod();

			void NonBlockingInterfaceMethod();
        }

		public abstract class Base {
			[Blocking]
			public abstract void BlockingAbstractMethod();

			[/* UnnecessaryBlocking(BlockingVirtualMethod) */ Blocking /**/]
			public virtual void BlockingVirtualMethod() { }

			public abstract void NonBlockingAbstractMethod();

			public virtual void NonBlockingVirtualMethod() { }
        }

		public sealed class Good : Base, IInterface {
			[Blocking]
			public void BlockingInterfaceMethod() {}

			public void NonBlockingInterfaceMethod() { }

			[Blocking]
            public override void BlockingAbstractMethod() {}

			[Blocking]
			public override void BlockingVirtualMethod() {}

			public override void NonBlockingAbstractMethod() {}
			public override void NonBlockingVirtualMethod() {}
        }

		public sealed class Bad : Base, IInterface {
			[Blocking]
			public void BlockingInterfaceMethod() {}

			[/* DontIntroduceBlockingInImplementation(Bad.NonBlockingInterfaceMethod,IInterface.NonBlockingInterfaceMethod) */ Blocking /**/]
			public void NonBlockingInterfaceMethod() {
				// Dummy call so that we don't get a diagnostic about how this method
				// doesn't need [Blocking] because it doesn't call anything that is
				// blocking.
				BasicTests.SomeBlockingMethod();
			}

			[Blocking]
            public override void BlockingAbstractMethod() {}

			// This one is info for a suggested edit, not an error.
			public override void /* NonBlockingImplementationOfBlockingThing(Bad.BlockingVirtualMethod,Base.BlockingVirtualMethod) */ BlockingVirtualMethod /**/ () {}

			[/* DontIntroduceBlockingInImplementation(Bad.NonBlockingAbstractMethod,Base.NonBlockingAbstractMethod) */ Blocking /**/]
			public override void NonBlockingAbstractMethod() {
				BasicTests.SomeBlockingMethod();
			}

			[/* DontIntroduceBlockingInImplementation(Bad.NonBlockingVirtualMethod,Base.NonBlockingVirtualMethod) */ Blocking /**/]
			public override void NonBlockingVirtualMethod() {
				BasicTests.SomeBlockingMethod();
			}

			public void NonBlockingInstanceMethod() {
				/* BlockingCallersMustBeBlocking(SomeBlockingMethod,NonBlockingInstanceMethod) */ BasicTests.SomeBlockingMethod() /**/;
            }
        }
	}
}
