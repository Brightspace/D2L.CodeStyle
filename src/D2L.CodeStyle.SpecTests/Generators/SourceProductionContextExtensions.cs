using System.Text;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.SpecTests.Generators {

	internal static class SourceProductionContextExtensions {

		public static void ReportException(
				this SourceProductionContext context,
				string generatorName,
				Exception exception
			) {

			Type exceptionType = exception.GetType();

			Diagnostic diagnostic = Diagnostic.Create(
					descriptor: Diagnostics.SourceGeneratorException,
					location: Location.None,
					messageArgs: new object[] {
						generatorName,
						exceptionType.FullName,
						exception.Message
					}
				);

			context.ReportDiagnostic( diagnostic );

			StringBuilder dump = new StringBuilder();
			dump.AppendLine( "/*" );
			dump.AppendLine( exception.ToString() );
			dump.AppendLine( "*/" );

			string hintName = $"{ exceptionType.Name }.{ Guid.NewGuid() }.cs";
			context.AddSource( hintName, dump.ToString() );
		}
	}
}
