using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Thread-local dictionary wrapper that can copy values to and from a shared static dictionary.
	/// </summary>
	public class DictionaryProvider<TKey, TValue>(Dictionary<TKey, TValue> staticDictionary) : IReadOnlyValueProvider<Dictionary<TKey, TValue>>, IDictionary<TKey, TValue> {
		private readonly Dictionary<TKey, TValue> _staticDictionary = staticDictionary;

		/// <summary>
		/// The shared dictionary backing this provider.
		/// </summary>
		public Dictionary<TKey, TValue> StaticSource => _staticDictionary;

		/// <summary>
		/// The local dictionary used by the current provider instance.
		/// </summary>
		public Dictionary<TKey, TValue> Value { get; } = [];

		/// <summary>
		/// Clears the shared static dictionary.
		/// </summary>
		public void ClearStatic() => _staticDictionary.Clear();

		/// <summary>
		/// Copies all shared static pairs into the local dictionary.
		/// </summary>
		public void CopyFromStatic() {
			Value.Clear();
			foreach (var (key, value) in _staticDictionary)
				Value.Add(key, value);
		}

		/// <summary>
		/// Copies all local pairs into the shared static dictionary.
		/// </summary>
		public void CopyToStatic() {
			foreach (var (key, value) in Value)
				_staticDictionary[key] = value;
		}

		#region IDictionary<TKey, TValue>
		/// <inheritdoc/>
		public TValue this[TKey key] { get => Value[key]; set => Value[key] = value; }

		/// <inheritdoc/>
		public ICollection<TKey> Keys => Value.Keys;

		/// <inheritdoc/>
		public ICollection<TValue> Values => Value.Values;

		/// <inheritdoc/>
		public int Count => Value.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)Value).IsReadOnly;

		/// <inheritdoc/>
		public void Add(TKey key, TValue value) => Value.Add(key, value);

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).Add(item);

		/// <inheritdoc/>
		public void Clear() => Value.Clear();

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).Contains(item);

		/// <inheritdoc/>
		public bool ContainsKey(TKey key) => Value.ContainsKey(key);

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).CopyTo(array, arrayIndex);

		/// <inheritdoc/>
		public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Value).GetEnumerator();

		/// <inheritdoc/>
		public bool Remove(TKey key) => Value.Remove(key);

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)Value).Remove(item);

		/// <inheritdoc/>
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Value.TryGetValue(key, out value);
		#endregion
	}
}
