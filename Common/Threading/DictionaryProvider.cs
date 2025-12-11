using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MagicStorage.Common.Threading {
	public class DictionaryProvider<TKey, TValue>(Dictionary<TKey, TValue> staticDictionary) : IReadOnlyValueProvider<Dictionary<TKey, TValue>>, IDictionary<TKey, TValue> {
		private readonly Dictionary<TKey, TValue> _staticDictionary = staticDictionary;

		public Dictionary<TKey, TValue> Value { get; } = [];

		public void ClearStatic() => _staticDictionary.Clear();

		public void CopyFromStatic() {
			Value.Clear();
			foreach (var (key, value) in _staticDictionary)
				Value.Add(key, value);
		}

		public void CopyToStatic() {
			foreach (var (key, value) in Value)
				_staticDictionary[key] = value;
		}

		#region IDictionary<TKey, TValue>
		public TValue this[TKey key] { get => Value[key]; set => Value[key] = value; }

		public ICollection<TKey> Keys => Value.Keys;

		public ICollection<TValue> Values => Value.Values;

		public int Count => Value.Count;

		public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)Value).IsReadOnly;

		public void Add(TKey key, TValue value) => Value.Add(key, value);

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).Add(item);

		public void Clear() => Value.Clear();

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).Contains(item);

		public bool ContainsKey(TKey key) => Value.ContainsKey(key);

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).CopyTo(array, arrayIndex);

		public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Value).GetEnumerator();

		public bool Remove(TKey key) => Value.Remove(key);

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).Remove(item);

		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Value.TryGetValue(key, out value);
		#endregion
	}
}
