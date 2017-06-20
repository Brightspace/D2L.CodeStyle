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

		private const string NotNullParamMethod = @"
namespace D2L.CodeStyle.Annotations.Contract {
	public class NotNullAttribute : System.Attribute {}
}

namespace Test {
	class TestProvider {
		public void TestMethod(
			[D2L.CodeStyle.Annotations.Contract.NotNull] string testName
		) {}

		public void TestMethod(
			object allowedToBeNull,
			[D2L.CodeStyle.Annotations.Contract.NotNull] string testName
		) {}

		public void TestMethodCanTakeNull( string testName ) {}

		public bool ShouldDoStuff => false;
	}
}
";

		private static readonly int NotNullParamMethodLines = NotNullParamMethod.Count( c => c.Equals( '\n' ) ) + 1;

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
					25
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
					13
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
					25
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
					25
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
					25
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
					25
				);
		}

		[Test]
		public void NotNullParam_NulLVariableIsInClosureContext_ReportsProblem() {
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
					44
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
		public void NotNullParam_VariablAssignedOtherVariable_AtDeclaration_DoesNotReportProblem() {
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
		public void NotNullParam_VariablAssignedOtherVariable_AfterDeclaration_DoesNotReportProblem() {
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

		#endregion

		private void AssertDoesNotProduceError(
			string file
		) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertProducesError(
			string file,
			int line,
			int column
		) {
			DiagnosticDescriptor descriptor = Diagnostics.NullPassedToNotNullParameter;
			DiagnosticResult result = new DiagnosticResult() {
				Id = descriptor.Id,
				Message = descriptor.MessageFormat.ToString(),
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

//namespace Test {
//	class TestCaller {
//		public void TestMethod() {
//			var provider = new TestProvider();
//			string name;
//			if( provider.ShouldDoStuff ) {
//				name = "Do some stuff";
//			} else {
//				name = "Do some other stuff";
//			}
//			provider.TestMethod( name );

//			string otherName = provider.ShouldDoStuff ? "Do?" : "Or do not?";
//			provider.TestMethod( otherName );
//		}
//	}
//}
