using System;
using D2L.CodeStyle.TestAnalyzers.Common;
using D2L.CodeStyle.TestAnalyzers.Test.Verifiers;
using D2L.LP.Extensibility.Activation.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.LP.Extensibility.Activation.Domain {

	public interface IServiceLocator { }
	public interface IDisposableServiceLocator { }

	public class ServiceLocator : IServiceLocator, IDisposableServiceLocator {

		public T Get<T>() {
			return default;
		}

	}

	public interface IDependencyRegistry { }
	public interface IOrgUnit { }
	public interface IOrganizationUser { }

	public interface IFoo { }

	public static class TestServiceLocatorFactory {

		// TestServiceLocatorFactory.Default (this is good)

		public static readonly IServiceLocator Default = default;

		// Bad TestServiceLocator Methods:

		public static IDisposableServiceLocator Create(
			Action<IDependencyRegistry> dependencyLoader
		) {
			return new ServiceLocator();
		}

		public static IDisposableServiceLocator Create(
			long orgId,
			Action<IDependencyRegistry> dependencyLoader = null
		) {
			return new ServiceLocator();
		}

		public static IDisposableServiceLocator Create(
			long orgId,
			long orgUnitId,
			Action<IDependencyRegistry> dependencyLoader = null
		) {
			return new ServiceLocator();
		}

		public static IDisposableServiceLocator Create(
			long orgId,
			long orgUnitId,
			long userId,
			Action<IDependencyRegistry> dependencyLoader = null
		) {
			return new ServiceLocator();
		}

		public static IDisposableServiceLocator Create(
			IOrgUnit orgUnit,
			IOrganizationUser user,
			Action<IDependencyRegistry> dependencyLoader = null
		) {
			return new ServiceLocator();
		}

	}

}

namespace D2L.CodeStyle.TestAnalyzers.ServiceLocator {

	[TestFixture]
	public class CustomTestServiceLocatorAnalyzerTests : DiagnosticVerifier {

		private static readonly MetadataReference NUnitReference = MetadataReference.CreateFromFile( typeof( TestAttribute ).Assembly.Location );
		private static readonly MetadataReference FactoryReference = MetadataReference.CreateFromFile( typeof( TestServiceLocatorFactory ).Assembly.Location );

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new CustomTestServiceLocatorAnalyzer( true );
		}

		protected override MetadataReference[] GetAdditionalReferences() {
			return new[] {
				NUnitReference,
				FactoryReference
			};
		}

		private const string PREAMBLE = @"
using System;
using D2L.LP.Extensibility.Activation.Domain;
";

		[Test]
		public void EmptyDocument_NoDiag() {
			const string test = @"";

			AssertNoDiagnostic( test );
		}

		[Test]
		public void TestServiceLocatorFactory_Default_NoDiag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
			IServiceLocator serviceLocator = TestServiceLocatorFactory.Default;
        }

    }
}";

			AssertNoDiagnostic( test );
		}

		// Test framework can't support this test quite yet since this analyzer uses additional files.
		// This should be uncommented if/when that functionality is in place.

		/*[Test]
		public void TestServiceLocatorFactory_Create_WhitelistedClass_NoDiag() {
			const string test = PREAMBLE + @"
namespace D2L.LE.ToolIntegration.Tests.Content.ContentDateSync.ContentTopic {
    class ContentTopicUpdatedSyncTests {

        public void Test() {
			long orgId = 3151;
			long orgUnitId = 4252;
			long userId = 73417;

			IOrgUnit orgUnit = null;
			IOrganizationUser user = null;

			Action<IDependencyRegistry> customAction = ( IDependencyRegistry registry ) => { };

			IDisposableServiceLocator serviceLocator1 = TestServiceLocatorFactory.Create(
				customAction
			);

			IDisposableServiceLocator serviceLocator2 = TestServiceLocatorFactory.Create(
				orgId
			);

			IDisposableServiceLocator serviceLocator3 = TestServiceLocatorFactory.Create(
				orgId,
				customAction
			);

			IDisposableServiceLocator serviceLocator4 = TestServiceLocatorFactory.Create(
				orgId: orgId,
				orgUnitId: orgUnitId
			);

			IDisposableServiceLocator serviceLocator5 = TestServiceLocatorFactory.Create(
				orgId,
				orgUnitId,
				userId
			);

			IDisposableServiceLocator serviceLocator6 = TestServiceLocatorFactory.Create(
				orgUnit,
				user
			);
        }

    }
}";

			AssertNoDiagnostic( test );
		}*/

		[Test]
		public void TestServiceLocatorFactory_Create_WithCustomAction_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
            Action<IDependencyRegistry> customAction = ( IDependencyRegistry registry ) => { };
			IDisposableServiceLocator serviceLocator = TestServiceLocatorFactory.Create( customAction );
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 10,
				column: 47
			);
		}

		[Test]
		public void TestServiceLocatorFactory_Create_WithOrgId_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
			long orgId = 6606;
			IDisposableServiceLocator serviceLocator = TestServiceLocatorFactory.Create(
				6606
			);
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 10,
				column: 47
			);
		}

		[Test]
		public void TestServiceLocatorFactory_Create_WithOrgIdCustomAction_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
            Action<IDependencyRegistry> customAction = ( IDependencyRegistry registry ) => { };
			IDisposableServiceLocator serviceLocator = TestServiceLocatorFactory.Create(
				6606,
				customAction
			);
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 10,
				column: 47
			);
		}

		[Test]
		public void TestServiceLocatorFactory_Create_OrgIdOrgUnitId_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
            long orgId = 3151;
			long orgUnitId = 4252;
			IDisposableServiceLocator serviceLocator = TestServiceLocatorFactory.Create(
				orgId: orgId,
				orgUnitId: orgUnitId
			);
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 11,
				column: 47
			);
		}


		[Test]
		public void TestServiceLocatorFactory_Create_OrgIdOrgUnitIdUserId_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
			long orgId = 3151;
			long orgUnitId = 4252;
			long userId = 73417;
			IDisposableServiceLocator serviceLocator = TestServiceLocatorFactory.Create(
				orgId,
				orgUnitId,
				userId
			);
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 12,
				column: 47
			);
		}


		[Test]
		public void TestServiceLocatorFactory_Create_WithContexts_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
            IOrgUnit orgUnit = null;
			IOrganizationUser user = null;
			IDisposableServiceLocator serviceLocator = TestServiceLocatorFactory.Create(
				orgUnit,
				user
			);
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 11,
				column: 47
			);
		}


		[Test]
		public void TestServiceLocatorFactory_Create_LazyWithCustomAction_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
            Action<IDependencyRegistry> customAction = ( IDependencyRegistry registry ) => { };
			Lazy<IDisposableServiceLocator> serviceLocator = TestServiceLocatorFactory.Create(
				customAction
			);
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 10,
				column: 53
			);
		}

		[Test]
		public void TestServiceLocatorFactory_Create_NotAssignedWithCustomAction_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
            Action<IDependencyRegistry> customAction = ( IDependencyRegistry registry ) => { };
			TestServiceLocatorFactory.Create(
				customAction
			);
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 10,
				column: 4
			);
		}

		[Test]
		public void TestServiceLocatorFactory_Create_Chained_Diag() {
			const string test = PREAMBLE + @"
namespace test {
    class Tests {

        public void Test() {
            Action<IDependencyRegistry> customAction = ( IDependencyRegistry registry ) => { };
			IFoo foo = TestServiceLocatorFactory.Create(
				customAction
			).Get<IFoo>();
        }

    }
}";

			AssertSingleDiagnostic(
				file: test,
				line: 10,
				column: 15
			);
		}

		private void AssertNoDiagnostic(
			string file
		) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic(
			string file,
			int line,
			int column
		) {
			DiagnosticResult result = new DiagnosticResult {
				Id = Diagnostics.CustomServiceLocator.Id,
				Message = Diagnostics.CustomServiceLocator.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};

			VerifyCSharpDiagnostic( file, result );
		}

	}
}