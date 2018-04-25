using D2L.CodeStyle.Analyzers.ApiUsage.JsonParamBinderAttribute;
using D2L.CodeStyle.Analyzers.Verifiers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	internal sealed class JsonParamBinderAnalyzerFixerTests : CodeFixVerifier {

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
			new JsonParamBinderAnalyzer();

		protected override CodeFixProvider GetCSharpCodeFixProvider() =>
			new JsonParamBinderAnalyzerFixer();


		private const string s_preamble = @"
using System;
using D2L.LP.Web.Rest.Attributes;
 
namespace D2L.LP.Web.Rest.Attributes {

	[AttributeUsage(
		AttributeTargets.Parameter,
		AllowMultiple = false,
		Inherited = true
	)]
	public class JsonConvertParameterBinder : Attribute {
	}

	[Obsolete( ""Prefer using JsonConvertParameterBinder for new APIs because it has saner behaviour"" )]
	[AttributeUsage(
		AttributeTargets.Parameter,
		AllowMultiple = false,
		Inherited = true
	)]
	public class JsonParamBinder : Attribute {
	}
}
";

		[Test]
		public void DocumentWithJsonParamBinder_ReplacedWithJsonConvertParameterBinder_ByCodeFixer() {
			const string testTemplate = s_preamble + @"
namespace test {
	sealed class Test {
		public void Foo( [ATTRIBUTE] string bar ) {}		
	}
}";
			string original = testTemplate.Replace( "ATTRIBUTE", "JsonParamBinder" );
			string expected = testTemplate.Replace( "ATTRIBUTE", "JsonConvertParameterBinder" );

			VerifyCSharpCodeFix( original, expected );
		}
	}
}
