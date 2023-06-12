// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.D2LPageAnalyzer

namespace D2L.Web
{
    public class D2LPage { }
}

namespace D2L.CodeStyle.Analyzers.ApiUsage
{
    public class OrdinaryClass { }

    public partial class OkayClass : D2L.Web.D2LPage { }

    public class /* D2LPageDerivedMustBePartial */ BasicTest /**/ : D2L.Web.D2LPage { }

    public class /* D2LPageDerivedMustBePartial */ DerivedClass /**/ : OkayClass { }
}
