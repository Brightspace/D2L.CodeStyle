using System;
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
using D2L.CodeStyle.Annotations.Contract;

namespace D2L.CodeStyle.Annotations.Contract {
	public class NotNullAttribute : System.Attribute {}

	public class NotNullWhenParameterAttribute : Attribute {}

	public class AllowNullAttribute : Attribute {}

	public class AlwaysAssignedValueAttribute : Attribute {
		public AlwaysAssignedValueAttribute( string variableName ) {
			VariableName = variableName;
		}
		public string VariableName { get; private set; }
	}
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

		private const string NotNullType = Attributes + @"
namespace Test {
	[NotNullWhenParameter]
	interface IDatabase {}

	class Database : IDatabase {};

	class TestProvider {
		public void TestMethod(
			IDatabase database
		) {}

		public void TestMethod(
			string allowedToBeNull,
			IDatabase testDatabase
		) {}

		public void MultiNotNull(
			IDatabase first,
			IDatabase second
		) {}

		public void TestMethodCanTakeNull(
			[AllowNull] IDatabase database
		) {}

		public bool ShouldDoStuff => false;
	}
}
";

		private static readonly int NotNullParamMethodLines = NotNullParamMethod.Count( c => c.Equals( '\n' ) ) + 1;
		private static readonly int NotNullTypeLines = NotNullType.Count( c => c.Equals( '\n' ) ) + 1;

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
		public void NotNullParam_AlwaysAssignedAttribute_VariableNeverAssigned_ReportsProblem() {
			const string test = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		[AlwaysAssignedValue( ""name"" )]
		public void TestMethod() {
			var provider = new TestProvider();
			string name;
			provider.TestMethod( name );
		}
	}
}";
			AssertProducesError(
					test,
					7 + NotNullParamMethodLines,
					25,
					"testName"
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
			// TODO: Modify the code to remove any blocks that contain a `return` and see if the value would be assigned?
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
		public void NotNullParam_VariableDeclaredAsAlwaysAssigned_DoesNotReportProblem() {
			// It was a case like this that needed adding the [AlwaysAssignedValue] attribute.
			// Make sure it still fails so we're actually testing that it makes a difference
			const string hasErrorTest = NotNullParamMethod + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			string hello;

			try {
				hello = provider.GetStuff();
			} catch( Exception e ) {
				return;
			}

			provider.TestMethod( hello );
		}
	}
}";
			AssertProducesError(
					hasErrorTest,
					13 + NotNullParamMethodLines,
					25,
					"testName"
				);

			string test = hasErrorTest.Replace(
					"public void TestMethod()",
					"[AlwaysAssignedValue(\"hello\")]public void TestMethod()"
				);
			AssertDoesNotProduceError( test );
		}

		#endregion

		#endregion

		#region Parameter's type has [NotNullWhenParameter]

		#region Should produce errors

		[Test]
		public void NotNullType_NullIsPassed_ReportsProblem() {
			const string test = NotNullType + @"
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
					5 + NotNullTypeLines,
					25,
					"database"
				);
		}

		[Test]
		public void NotNullType_NullIsPassed_MethodInSameClass_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			DoStuff( null );
		}

		public void DoStuff(
			IDatabase database
		) {}
	}
}";
			AssertProducesError(
					test,
					4 + NotNullTypeLines,
					13,
					"database"
				);
		}

		[Test]
		public void NotNullType_NullVariableIsPassed_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase name = null;
			provider.TestMethod( name );
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullTypeLines,
					25,
					"database"
				);
		}

		[Test]
		public void NotNullType_NullVariableIsPassedInConstructor_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public TestCaller() {
			var provider = new TestProvider();
			IDatabase db = null;
			provider.TestMethod( db );
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullTypeLines,
					25,
					"database"
				);
		}

		[Test]
		public void NotNullType_RealValueAssignedAfterPassing_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db;
			provider.TestMethod( db );
			db = new Database();
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullTypeLines,
					25,
					"database"
				);
		}

		[Test]
		public void NotNullType_VariableNotAlwaysAssignedValue_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db;
			if( provider.ShouldDoStuff ) {
				db = new Database();
			}
			provider.TestMethod( db );
		}
	}
}";
			AssertProducesError(
					test,
					9 + NotNullTypeLines,
					25,
					"database"
				);
		}

		[Test]
		public void NotNullType_VariableNotAlwaysAssignedNonNullValue_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db = null;
			if( provider.ShouldDoStuff ) {
				db = new Database();
			}
			provider.TestMethod( db );
		}
	}
}";
			AssertProducesError(
					test,
					9 + NotNullTypeLines,
					25,
					"database"
				);
		}

		[Test]
		public void NotNullType_NullVariableIsInClosureContext_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db = null;
			var action = () => provider.TestMethod( db );
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullTypeLines,
					44,
					"database"
				);
		}

		[Test]
		public void NotNullType_MultipleParamtersWithIssue_ReportsMultipleProblems() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db = null;
			provider.TestMethod( new Database() );
			provider.MultiNotNull(
					db,
					null
				);
		}
	}
}";
			DiagnosticDescriptor descriptor = Diagnostics.NullPassedToNotNullParameter;
			DiagnosticResult[] expectedResults = new[] {
				new DiagnosticResult() {
					Id = descriptor.Id,
					Message = string.Format( descriptor.MessageFormat.ToString(), "first" ),
					Severity = DiagnosticSeverity.Error,
					Locations = new[] {
						new DiagnosticResultLocation( "Test0.cs", 8 + NotNullTypeLines, 6 )
					}
				},
				new DiagnosticResult() {
					Id = descriptor.Id,
					Message = string.Format( descriptor.MessageFormat.ToString(), "second" ),
					Severity = DiagnosticSeverity.Error,
					Locations = new[] {
						new DiagnosticResultLocation( "Test0.cs", 9 + NotNullTypeLines, 6 )
					}
				}
			};

			VerifyCSharpDiagnostic( test, expectedResults );
		}

		[Test]
		public void NotNullType_NamedArguments_OneIsNull_ReportsProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod(
				testDatabase: null,
				allowedToBeNull: ""This one is actually a string, not a database""
			);
		}
	}
}";
			AssertProducesError(
					test,
					6 + NotNullTypeLines,
					5,
					"testDatabase"
				);
		}

		#endregion

		#region  Should not produce errors, due to not passing null

		[Test]
		public void NotNullType_ValueIsPassed_DoesNotReportProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( new Database() );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullType_VariableWithValueAssigned_AtDeclaration_DoesNotReportProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db = new Database();
			provider.TestMethod( db );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullType_VariableAssignedOtherVariable_AfterDeclaration_DoesNotReportProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod( IDatabase intakeDb ) {
			var provider = new TestProvider();
			IDatabase db;
			db = intakeDb;
			provider.TestMethod( db );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullType_OneParamNotNull_NullPassedToNullable_DoesNotReportProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			provider.TestMethod( null, new Database() );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		#endregion

		#region Parameter is flagged with [AllowNull]

		[Test]
		public void NotNullType_AllowNull_NullIsPassed_DoesNotReportProblem() {
			const string test = NotNullType + @"
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
		public void NotNullType_AllowNull_NullIsPassed_MethodInSameClass_DoesNotReportProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			DoStuff( null );
		}

		public void DoStuff(
			[AllowNull] IDatabase database
		) {}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullType_AllowNull_VariableNotAlwaysAssignedValue_DoesNotReportProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db;
			if( provider.ShouldDoStuff ) {
				db = new Database();
			}
			provider.TestMethodCanTakeNull( db );
		}
	}
}";
			AssertDoesNotProduceError( test );
		}

		[Test]
		public void NotNullType_AllowNull_VariableNotAlwaysAssignedNonNullValue_DoesNotReportProblem() {
			const string test = NotNullType + @"
namespace Test {
	class TestCaller {
		public void TestMethod() {
			var provider = new TestProvider();
			IDatabase db = null;
			if( provider.ShouldDoStuff ) {
				db = new Database();
			}
			provider.TestMethodCanTakeNull( db );
		}
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
