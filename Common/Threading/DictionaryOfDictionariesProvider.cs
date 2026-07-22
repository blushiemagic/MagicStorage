using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Mirrors a static nested dictionary into a mutable working dictionary.
	/// </summary>
	/// <typeparam name="TKeyOuter">The outer dictionary key type.</typeparam>
	/// <typeparam name="TKeyInner">The inner dictionary key type.</typeparam>
	/// <typeparam name="TValue">The stored value type.</typeparam>
	public class DictionaryOfDictionariesProvider<TKeyOuter, TKeyInner, TValue> : IReadOnlyValueProvider<Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>>>, IDictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> {
		private readonly Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> _staticDictionary;

		/// <inheritdoc />
		public Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> StaticSource => _staticDictionary;

		/// <inheritdoc />
		public Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> Value { get; } = new();

		/// <summary>
		/// Creates a provider for the specified static nested dictionary.
		/// </summary>
		/// <param name="staticDictionary">The static dictionary mirrored by this provider.</param>
		public DictionaryOfDictionariesProvider(Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> staticDictionary) {
			_staticDictionary = staticDictionary;
		}

		/// <summary>
		/// Gets or sets a value in the working dictionary by outer and inner key.
		/// </summary>
		/// <param name="keyOuter">The outer dictionary key.</param>
		/// <param name="keyInner">The inner dictionary key.</param>
		/// <returns>The stored value for the nested key pair.</returns>
		public TValue this[TKeyOuter keyOuter, TKeyInner keyInner] {
			get => Value[keyOuter][keyInner];
			set {
				if (!Value.TryGetValue(keyOuter, out var innerDict))
					Value[keyOuter] = innerDict = [];
				innerDict[keyInner] = value;
			}
		}

		/// <inheritdoc />
		public void ClearStatic() => _staticDictionary.Clear();

		/// <inheritdoc />
		public void CopyFromStatic() {
			Value.Clear();
			foreach (var (key, innerDict) in _staticDictionary)
				Value.Add(key, new Dictionary<TKeyInner, TValue>(innerDict));
		}

		/// <inheritdoc />
		public void CopyToStatic() {
			foreach (var (key, innerDict) in Value)
				_staticDictionary[key] = new Dictionary<TKeyInner, TValue>(innerDict);
		}

		#region IDictionary<TKeyOuter, Dictionary<TKeyInner, TValue>>
		/// <inheritdoc />
		public Dictionary<TKeyInner, TValue> this[TKeyOuter key] { get => Value[key]; set => Value[key] = value; }

		/// <inheritdoc />
		public ICollection<TKeyOuter> Keys => Value.Keys;

		/// <inheritdoc />
		public ICollection<Dictionary<TKeyInner, TValue>> Values => Value.Values;

		/// <inheritdoc />
		public int Count => Value.Count;

		/// <inheritdoc />
		public bool IsReadOnly => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).IsReadOnly;

		/// <inheritdoc />
		public void Add(TKeyOuter key, Dictionary<TKeyInner, TValue> value) => Value.Add(key, value);

		void ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.Add(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>> item) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).Add(item);

		/// <inheritdoc />
		public void Clear() => Value.Clear();

		bool ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.Contains(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>> item) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).Contains(item);

		/// <inheritdoc />
		public bool ContainsKey(TKeyOuter key) => Value.ContainsKey(key);

		void ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.CopyTo(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>> IEnumerable<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc />
		public bool Remove(TKeyOuter key) => Value.Remove(key);

		bool ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.Remove(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>> item) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).Remove(item);

		/// <inheritdoc />
		public bool TryGetValue(TKeyOuter key, out Dictionary<TKeyInner, TValue> value) => Value.TryGetValue(key, out value);
		#endregion
	}
}
