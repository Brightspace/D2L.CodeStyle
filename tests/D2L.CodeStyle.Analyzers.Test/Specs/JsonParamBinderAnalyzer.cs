﻿// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.JsonParamBinderAttribute.JsonParamBinderAnalyzer, D2L.CodeStyle.Analyzers

using System;
using D2L.CodeStyle.Analyzers.ApiUsage.JsonParamBinderAttribute;
using D2L.CodeStyle.Annotations;
using D2L.LP.Web.Rest.Attributes;

namespace D2L.LP.Web.Rest.Attributes {

	public sealed class JsonParamBinder : Attribute { }

	public sealed class OtherAttribute : Attribute { }

}

namespace SpecTests {

	public class TestClass {

		public void Test( int x ) { } // No diagnostic because no attribute

		public void Test2( [/* ObsoleteJsonParamBinder */ JsonParamBinder /**/] int x ) { }

		public void Test3( string x, [/* ObsoleteJsonParamBinder */ JsonParamBinder /**/] int y ) { }

		public void Test4( string x, [/* ObsoleteJsonParamBinder */ JsonParamBinder /**/] int y = 0 ) { }

		public void Test5( [OtherAttribute, /* ObsoleteJsonParamBinder */ JsonParamBinder /**/] int x ) { }

		public void Test6( [OtherAttribute][/* ObsoleteJsonParamBinder */ JsonParamBinder /**/] int x ) { }

	}

	public class AllowedClass {

		public void Test( [JsonParamBinder] int x ) { } // No diagnostic because allowed legacy class

		public void Test( [JsonParamBinder, OtherAttribute] int x ) { } // No diagnostic because allowed legacy class

	}

	public class /* UnnecessaryAllowedListEntry(SpecTests.AllowedClass2\, JsonParamBinderAnalyzer, LegacyJsonParamBinderAllowedList.txt) */ AllowedClass2 /**/ {

		public void Test( int x ) { }

	}

}
