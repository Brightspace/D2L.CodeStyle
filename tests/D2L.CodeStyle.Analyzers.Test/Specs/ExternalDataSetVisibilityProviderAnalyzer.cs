// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.DataHub.ExternalDataSetVisibilityProviderAnalyzer

using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace D2L.AW.DataExport.BrightspaceDataSets.Domain {
	internal interface IEventDrivenDataSetPlugin { }
	internal interface IExternalDataSetVisibilityProvider<T> where T : IEventDrivenDataSetPlugin { }
}

namespace D2L.CodeStyle.Analyzers.Tests.Specs {
	using D2L.AW.DataExport.BrightspaceDataSets.Domain;

	class ValidConstructorParameter : IEventDrivenDataSetPlugin {
		ValidConstructorParameter(
			IExternalDataSetVisibilityProvider<ValidConstructorParameter> e
		) { }
	}

	class InvalidConstructorParameter : IEventDrivenDataSetPlugin{
		InvalidConstructorParameter(
			/* ExternalDataSetVisibilityProviderTypeParameterMatchesClass */ IExternalDataSetVisibilityProvider<ValidConstructorParameter> /**/ e 
		) { }
	}
}

