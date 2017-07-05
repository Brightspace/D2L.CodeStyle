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
		public void NotNullParam_NullVariableIsPassed_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name = null;
			provider.TestMethod( name );
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
		public void NotNullParam_NullVariableIsPassedInConstructor_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public TestCaller() {
			var provider = new TestProvider();
			string name = null;
			provider.TestMethod( name );
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
		public void NotNullParam_RealValueAssignedAfterPassing_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name;
			provider.TestMethod( name );
			name = ""Antidisestablishmentarianism"";
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
		public void NotNullParam_VariableNotAlwaysAssignedValue_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name;
			if( provider.ShouldDoStuff ) {
				name = ""Antidisestablishmentarianism"";
			}
			provider.TestMethod( name );
		}
	}
}";
			AssertProducesError(
					test,
					9 + NotNullParamMethodLines,
					25,
					"testName"
				);
		}

		[Test]
		public void NotNullParam_VariableNotAlwaysAssignedNonNullValue_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name = null;
			if( provider.ShouldDoStuff ) {
				name = ""Antidisestablishmentarianism"";
			}
			provider.TestMethod( name );
		}
	}
}";
			AssertProducesError(
					test,
					9 + NotNullParamMethodLines,
					25,
					"testName"
				);
		}

		[Test]
		public void NotNullParam_NullVariableIsInClosureContext_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name = null;
			var action = () => provider.TestMethod( name );
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullParamMethodLines,
					44,
					"testName"
				);
		}

		[Test]
		public void NotNullParam_MultipleParamtersWithIssue_ReportsMultipleProblems() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name = null;
			provider.TestMethod( ""Hello!"" );
			provider.MultiNotNull(
					name,
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
						new DiagnosticResultLocation( "Test0.cs", 8 + NotNullParamMethodLines, 6 )
					}
				},
				new DiagnosticResult() {
					Id = descriptor.Id,
					Message = string.Format( descriptor.MessageFormat.ToString(), "anotherName" ),
					Severity = DiagnosticSeverity.Error,
					Locations = new[] {
						new DiagnosticResultLocation( "Test0.cs", 9 + NotNullParamMethodLines, 6 )
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
		public void NotNullParam_VariableDeclaredInDelegate_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {

		delegate void MyDelegate( int number );

		public void TestMethod() {
			var provider = new TestProvider();

			MyDelegate delegate = delegate( int num ) {
				string val = null;
				provider.TestMethod( num, val );
			};
		}
	}
}";
			AssertProducesError(
					test,
					11 + NotNullParamMethodLines,
					31,
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
			string hello = null;
			DoSomeStuff( provider, hello );
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
					6 + NotNullParamMethodLines,
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
			string hello = null;
			DoSomeStuff(
					provider,
					anotherValue: hello
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
					8 + NotNullParamMethodLines,
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
		public void NotNullParam_VariableWithValueAssigned_AfterDeclaration_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name;
			name = ""Supercalifragilisticexpialidocious""
			provider.TestMethod( name );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_VariableAssignedOtherVariable_AtDeclaration_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod( string intakeName ) {
			var provider = new TestProvider();
			string name = intakeName;
			provider.TestMethod( name );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_VariableAssignedOtherVariable_AfterDeclaration_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod( string intakeName ) {
			var provider = new TestProvider();
			string name;
			name = intakeName;
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
		public void NotNullParam_VariableAlwaysAssignedValue_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name;
			if( provider.ShouldDoStuff ) {
				name = ""Do some stuff"";
			} else {
				name = ""Do some other stuff"";
			}
			provider.TestMethod( name );

			string otherName = provider.ShouldDoStuff ? ""Do?"" : ""Or do not?"";
			provider.TestMethod( otherName );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_NullVariableAtDeclaration_AlwaysAssignedValueAfterDeclaration_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string name = null;
			if( provider.ShouldDoStuff ) {
				name = ""Do some stuff"";
			} else {
				name = ""Do some other stuff"";
			}
			provider.TestMethod( name );

			string otherName = null;
			otherName = provider.ShouldDoStuff ? ""Do?"" : ""Or do not?"";
			provider.TestMethod( otherName );
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
		public void NotNullParam_VariableUsedInNestedBlock_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			if( provider.ShouldDoStuff ) {
				string value = provider.GetStuff();
				for( int i = 0; i < 5; i++ ) {
					provider.TestMethod( value );
				}
			}
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
		public void NotNullParam_DeclaredAfterPotentialReturn_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			if( provider.ShouldDoStuff ) {
				return;
			}

			string tempTwo = provider.GetStuff();
			provider.TestMethod( tempTwo );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_NullVariablePassedWithNullCoalesce_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string hello = null;
			provider.TestMethod( hello ?? ""Testing, testing, one two three"" );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_AssignedNonNullAfterPotentialReturn_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string hello = null;
			if( provider.ShouldDoStuff ) {
				return;
			}

			hello = provider.GetStuff();
			provider.TestMethod( hello );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_AssignedAfterPotentialReturn_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string hello;

			if( provider.ShouldDoStuff ) {
				return;
			}

			hello = provider.GetStuff();
			provider.TestMethod( hello );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullParam_ValueUsedInDelegate_DoesNotReportProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {

		delegate void MyDelegate( int number, string value );

		public void TestMethod() {
			var provider = new TestProvider();

			MyDelegate delegate = delegate( int num, string val ) {
				provider.TestMethod( num, val );
			};
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
