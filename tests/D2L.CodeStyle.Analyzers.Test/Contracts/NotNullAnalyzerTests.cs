using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using D2L.CodeStyle.Analyzers.Contract;
using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Contracts {

	[TestFixture]
	internal sealed class NotNullAnalyzerTests : DiagnosticVerifier {

		private const string Attributes = @"
using System;
using D2L.CodeStyle.Annotations.Contract;

namespace D2L.CodeStyle.Annotations.Contract {
	public class NotNullAttribute : System.Attribute {}
}
";

		private const string NotNullParamMethod = Attributes + @"
namespace Test {
	class TestProvider {
		public void TestMethod(
			[NotNull] string testName
		) {}

		public void TestMethod(
			object allowedToBeNull,
			[NotNull] string testName
		) {}

		public void MultiNotNull(
			[NotNull] string testName,
			[NotNull] string anotherName
		) {}

		public string GetStuff() { return ""Ghostbusters"" };

		public void TestMethodCanTakeNull( string testName ) {}

		public bool ShouldDoStuff => false;
	}
}
";

		private static readonly int NotNullParamMethodLines = NotNullParamMethod.Count( c => c.Equals( '\n' ) ) + 1;

		#region Parameter has [NotNull] directly

		#region Should produce errors

		[Test]
		public void NotNullParam_NullIsPassed_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( null );
		}
	}
}";
			AssertProducesError(
					test,
					5 + NotNullParamMethodLines,
					25,
					"testName"
				);
		}

		[Test]
		public void NotNullParam_NullIsPassed_MethodInSameClass_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			DoStuff( null );
		}

		public void DoStuff(
			[D2L.CodeStyle.Annotations.Contract.NotNull] string stuff
		) {}
	}
}";
			AssertProducesError(
					test,
					4 + NotNullParamMethodLines,
					13,
					"stuff"
				);
		}

		[Test]
		public void NotNullParam_MultipleParametersWithIssue_ReportsMultipleProblems() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( ""Hello!"" );
			provider.MultiNotNull(
					null,
					null
				);
		}
	}
}";
			DiagnosticDescriptor descriptor = Diagnostics.NullPassedToNotNullParameter;
			DiagnosticResult[] expectedResults = new[] {
				new DiagnosticResult() {
					Id = descriptor.Id,
					Message = string.Format( descriptor.MessageFormat.ToString(), "testName" ),
					Severity = DiagnosticSeverity.Error,
					Locations = new[] {
						new DiagnosticResultLocation( "Test0.cs", 7 + NotNullParamMethodLines, 6 )
					}
				},
				new DiagnosticResult() {
					Id = descriptor.Id,
					Message = string.Format( descriptor.MessageFormat.ToString(), "anotherName" ),
					Severity = DiagnosticSeverity.Error,
					Locations = new[] {
						new DiagnosticResultLocation( "Test0.cs", 8 + NotNullParamMethodLines, 6 )
					}
				}
			};

			VerifyCSharpDiagnostic( test, expectedResults );
		}

		[Test]
		public void NotNullParam_NamedArguments_OneIsNull_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod(
				testName: null,
				allowedToBeNull: ""This is an object""
			);
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullParamMethodLines,
					5,
					"testName"
				);
		}

		[Test]
		public void NotNullParam_NamedArgumentsWithAtSign_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			DoSomeStuff(
				@char: null,
				myVar: ""A Value""
			);
		}

		private void DoSomeStuff(
			[NotNull] string @char,
			[NotNull] string myVar
		) {}
	}
}";
			AssertProducesError(
					test,
					5 + NotNullParamMethodLines,
					5,
					"char"
				);
		}

		[Test]
		public void NotNullParam_CalledTwice_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( ""This is a value"" );
			provider.TestMethod( null );
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullParamMethodLines,
					25,
					"testName"
				);
		}

		[Test]
		public void NotNullParam_CalledMethodHasDefaultValues_NullValuePassed_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			DoSomeStuff( provider, null );
		}

		private void DoSomeStuff(
			Provider provider,
			[NotNull] string value = ""a value"",
			[NotNull] string anotherValue = ""another value""
		) {}
	}
}";
			AssertProducesError(
					test,
					5 + NotNullParamMethodLines,
					27,
					"value"
				);
		}

		[Test]
		public void NotNullParam_CalledMethodHasDefaultValues_NamedArgumentsWithNull_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			DoSomeStuff(
					provider,
					anotherValue: null
				);
		}

		private void DoSomeStuff(
			TestProvider provider,
			[NotNull] string value = ""a value"",
			[NotNull] string anotherValue = ""another value""
		) {}
	}
}";
			AssertProducesError(
					test,
					7 + NotNullParamMethodLines,
					6,
					"anotherValue"
				);
		}

		[Test]
		public void NotNullParam_OverloadedMethodHasDefaultValues_NamedArgumentsWithNull_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			DoSomeStuff(
					value: null,
					provider: provider
				);
		}

		private void DoSomeStuff(
			string provider = ""a value"",
			[NotNull] string value = ""another value""
		) {}

		private void DoSomeStuff(
			TestProvider provider,
			[NotNull] string value = ""a value""
		) {}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullParamMethodLines,
					6,
					"value"
				);
		}

		#endregion

		#region Should not produce errors

		[Test]
		public void NotNullParam_ValueIsPassed_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( ""My Name"" );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_VariableWithValueAssigned_AtDeclaration_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name = ""Hello!"";
			provider.TestMethod( name );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_MethodParameterPassed_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod( string intakeName ) {
			var provider = new TestProvider();
			provider.TestMethod( intakeName );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_NoAttribute_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethodCanTakeNull( null );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_OneParamNotNull_NullPassedToNullable_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( null, ""This is the not-nullable one"" );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_MethodCallResultPassedDirectly_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( provider.GetStuff() );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_CalledMethodHasDefaultValues_MarkedAsNotNull_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string hello = ""something"";
			DoSomeStuff( provider, hello );
		}

		private void DoSomeStuff(
			TestProvider provider,
			[NotNull] string value = ""a value"",
			[NotNull] string anotherValue = ""another value"",
		) {}
	}
}";
			AssertDoesNotProduceError( test );
		}
		

		#endregion

		#endregion

		private void AssertDoesNotProduceError(
			string file
		) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertProducesError(
			string file,
			int line,
			int column,
			string paramName
		) {
			DiagnosticDescriptor descriptor = Diagnostics.NullPassedToNotNullParameter;
			DiagnosticResult result = new DiagnosticResult() {
				Id = descriptor.Id,
				Message = string.Format( descriptor.MessageFormat.ToString(), paramName ),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column ),
				}
			};

			VerifyCSharpDiagnostic( file, result );
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new NotNullAnalyzer();
		}

	}
}
