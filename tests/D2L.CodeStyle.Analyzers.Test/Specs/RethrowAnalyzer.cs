// analyzer: D2L.CodeStyle.Analyzers.Language.RethrowAnalyzer

using System;

namespace SpecTests {
	public sealed class SpecTests {

		public void Foo() {

			try {
				Bar();
			} catch( Exception e ) {
				throw;
			}

			try {
				Bar();
			} catch( Exception e ) {
				/* ShouldRethrow */ throw e; /**/
			}

			try {
				Bar();
			} catch( Exception e ) {
				e = new Exception();
				throw e;
			}

			try {
				Bar();
			} catch( Exception ) {
				throw;
			}

			try {
				Bar();
			} catch( Exception ) { }

			try {
				Bar();
			} catch {
				throw;
			}

			try {
				Bar();
			} catch { }

		}

		public void Bar();

	}
}
