using System;
using System.Collections.Generic;

namespace MagicStorage.Common.Collections {
	/// <summary>
	/// Extension methods that project keys through dictionary lookups.
	/// </summary>
	public static class EnumeratorExtensions {
		/// <summary>
		/// Projects each key in <paramref name="source"/> to the matching dictionary value.
		/// </summary>
		public static IEnumerable<TValue> SelectByDictionary<TKey, TValue>(this IEnumerable<TKey> source, Dictionary<TKey, TValue> dictionary) {
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(dictionary);

			return new DictionarySelectByKeyEnumerator<TKey, TValue>(source, dictionary);
		}

		/// <summary>
		/// Projects each key in <paramref name="source"/> to a dictionary collection and flattens the values.
		/// </summary>
		public static IEnumerable<TValue> SelectManyByDictionary<TKey, TCollection, TValue>(this IEnumerable<TKey> source, Dictionary<TKey, TCollection> dictionary)
			where TCollection : IEnumerable<TValue>
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(dictionary);

			return new DictionarySelectManyByKeyEnumerator<TKey, TCollection, TValue>(source, dictionary);
		}
	}
}
