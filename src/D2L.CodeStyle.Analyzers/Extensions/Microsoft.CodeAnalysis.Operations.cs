using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Extensions;

internal static partial class RoslynExtensions {

	public static IOperation UnwrapConversions( this IOperation op ) => op switch {
		IConversionOperation conversion => UnwrapConversions( conversion.Operand ),
		_ => op
	};

}
