using System;
using System.Collections.Generic;

namespace D2L.CodeStyle.UnsafeStaticCounter {

	internal static class DictionaryExtensions {

		internal static TValue GetOrAdd<TKey, TValue>(
				this IDictionary<TKey, TValue> dictionary,
				TKey key,
				Func<TValue> getter
			) {

			TValue value;
			if( dictionary.TryGetValue( key, out value ) ) {
				return value;
			}

			value = getter();
			dictionary.Add( key, value );
			return value;
		}
	}
}
