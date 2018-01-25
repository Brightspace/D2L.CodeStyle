using D2L.CodeStyle.Analyzers.Test.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	[TestFixture]
	internal sealed class RpcDependencyAnalyzerTests : DiagnosticVerifier {
		private const string PREAMBLE = @"
namespace D2L.Web {
	interface IRpcContext {}
	interface IRpcPostContext {}
	class RpcAttribute : System.Attribute {}
}

namespace D2L.Web.RequestContext {
	interface IRpcPostContextBase {}
}

namespace D2L.LP.Extensibility.Activation.Domain {
	class DependencyAttribute : System.Attribute {}
}";

		[Test]
		public void NormalMethod_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		public void Test( int x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void NormalMethodWithUnknownAttribute_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class Test {
		[SomethingWeird]
		public void Test( int x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void MethodWithUnrelatedButAnnoyinglyNamedRpcAttribute_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	class RpcAttribute : System.Attribute {} // shadow the parent one
	class Test {
		[Rpc]
		public void Test() {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void MethodWithRpcAttributeButNoArguments_RpcContextDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;

	class Test {
		[Rpc]
		public void Test() {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.RpcContextFirstArgument, test, 20, 19 );
		}

		[Test]
		public void MethodWithRpcAttributeAndIntFirstArgument_RpcContextDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;

	class Test {
		[Rpc]
		public void Test( int x ) {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.RpcContextFirstArgument, test, 20, 21 );
		}

		[Test]
		public void RpcWithFirstArgumentOfUnknownType_Diag() {
			// This test ensures that we don't throw a NullReferenceException due to the first argument being of unknown type
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( SomethingWeird x ) {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.RpcContextFirstArgument, test, 20, 21 );
		}

		[Test]
		public void MethodWithRpcAttributeAndIRpcContextFirstArg_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;

	class Test {
		[Rpc]
		public void Test( IRpcContext x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void MethodWithRpcAttributeAndIRpcPostContextFirstArg_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;

	class Test {
		[Rpc]
		public void Test( IRpcPostContext x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void MethodWithRpcAttributeAndIRpcPostContextBaseFirstArg_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.Web.RequestContext;

	class Test {
		[Rpc]
		public void Test( IRpcPostContextBase x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void RpcWithoutDependencyArgs_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( IRpcContext x, int x, int y ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void RpcWithParameterThatHasBadAttributeType_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( IRpcContext x, [SomethingWeird] int x, int y ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void RpcWithSingleDependencyNoParams_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( IRpcContext x, [Dependency] int x ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void RpcWithTwoDependencyNoParams_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( IRpcContext x, [Dependency] int x, [Dependency] int y ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void RpcWithTwoDependencySingleParams_NoDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( IRpcContext x, [Dependency] int x, [Dependency] int y, int z ) {}
	}
}";
			AssertNoDiagnostic( test );
		}

		[Test]
		public void RpcWithDependencyAtEnd_SortDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( IRpcContext x, int y, int z, [Dependency] int a ) {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.RpcArgumentSortOrder, test, 20, 50 );
		}

		[Test]
		public void RpcWithTwoDependencySingleParamInTheMiddle_SortDiag() {
			const string test = PREAMBLE + @"
namespace Test {
	using D2L.Web;
	using D2L.LP.Extensibility.Activation.Domain;
	class Test {
		[Rpc]
		public void Test( IRpcContext x, [Dependency] int y, int z, [Dependency] int w ) {}
	}
}";
			AssertSingleDiagnostic( Diagnostics.RpcArgumentSortOrder, test, 20, 63 );
		}

		private void AssertNoDiagnostic( string file ) {
			VerifyCSharpDiagnostic( file );
		}

		private void AssertSingleDiagnostic( DiagnosticDescriptor diag, string file, int line, int column ) {
			DiagnosticResult result = new DiagnosticResult {
				Id = diag.Id,
				Message = diag.MessageFormat.ToString(),
				Severity =  DiagnosticSeverity.Error,
				Locations = new [] {
					new DiagnosticResultLocation( "Test0.cs", line, column )
				}
			};

			VerifyCSharpDiagnostic( file, result );
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new RpcAnalyzer();
		}
	}
}
