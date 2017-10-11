// analyzer: D2L.CodeStyle.Analyzers.AspThreadAbortExceptions.AspThreadAbortExceptionsAnalyzer

using System.Security.Policy;
using System.Web;

namespace System.Web {
	public sealed class HttpResponse {
		public static void Redirect( string url );
		public static void Redirect( string url, bool endResponse );
		public static void End();
	}
}

namespace Tests {
	internal sealed class Tests {
		public void SafeThings() {
			var response = new HttpResponse();

			response.Redirect( "something", false );
			response.Redirect( "something", endResponse: false );

			// broken, but shouldn't emit a diagnostic
			response.Redirect( "hey", endResponse: 22 );

			response.Redirect( endResponse: false, url: "weird case" );
		}

		public void UnsafeThings() {
			var response = new HttpResponse();

			/* DontUseAspResponseEnd */ response.End() /**/ ;
			/* UnsafeUseOfAspRedirect */ response.Redirect( "hey" ) /**/ ;
			/* UnsafeUseOfAspRedirect */ response.Redirect( "hey", true ) /**/ ;
			/* UnsafeUseOfAspRedirect */ response.Redirect( "hey", endResponse: true ) /**/ ;
			/* UnsafeUseOfAspRedirect */ response.Redirect( endResponse: true, url: "weird case" ) /**/ ;

		}
	}
}
