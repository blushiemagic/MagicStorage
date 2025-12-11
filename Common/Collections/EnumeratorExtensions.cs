using System;
using System.Collections.Generic;

namespace MagicStorage.Common.Collections {
	public static class EnumeratorExtensions {
		public static IEnumerable<TValue> SelectByDictionary<TKey, TValue>(this IEnumerable<TKey> source, Dictionary<TKey, TValue> dictionary) {
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(dictionary);

			return new DictionarySelectByKeyEnumerator<TKey, TValue>(source, dictionary);
		}

		public static IEnumerable<TValue> SelectManyByDictionary<TKey, TCollection, TValue>(this IEnumerable<TKey> source, Dictionary<TKey, TCollection> dictionary)
			where TCollection : IEnumerable<TValue>
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(dictionary);

			return new DictionarySelectManyByKeyEnumerator<TKey, TCollection, TValue>(source, dictionary);
		}
	}
}
