using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers {
	internal static partial class Attributes {
		internal sealed class ImmutableAttribute : RoslynAttribute {
			private const string InheritedPropertyName = "Inherited";
			private const bool DefaultInheritedValue = true;

			public ImmutableAttribute()
				: base( "D2L.CodeStyle.Annotations.Objects.Immutable" ) { }

			public bool GetValueForInherited( ISymbol s ) {
				if( !IsDefined( s ) ) {
					throw new InvalidOperationException(
						"cannot call GetValueForInherited on a symbol on which the attribute is not defined"
					);
				}

				var definition = GetAll( s ).First();
				foreach( var arg in definition.NamedArguments ) {
					if( arg.Key != InheritedPropertyName ) {
						continue;
					}

					var value = arg.Value.Value;
					if( value is bool ) {
						// the value may not be a bool, as in the middle of typing
						return (bool)value;
					}
				}
				return DefaultInheritedValue;
			}
		}
	}
}
