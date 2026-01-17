using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	public class DictionaryOfDictionariesProvider<TKeyOuter, TKeyInner, TValue> : IReadOnlyValueProvider<Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>>>, IDictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> {
		private readonly Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> _staticDictionary;

		public Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> StaticSource => _staticDictionary;

		public Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> Value { get; } = new();

		public DictionaryOfDictionariesProvider(Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>> staticDictionary) {
			_staticDictionary = staticDictionary;
		}

		public TValue this[TKeyOuter keyOuter, TKeyInner keyInner] {
			get => Value[keyOuter][keyInner];
			set {
				if (!Value.TryGetValue(keyOuter, out var innerDict))
					Value[keyOuter] = innerDict = [];
				innerDict[keyInner] = value;
			}
		}

		public void ClearStatic() => _staticDictionary.Clear();

		public void CopyFromStatic() {
			Value.Clear();
			foreach (var (key, innerDict) in _staticDictionary)
				Value.Add(key, new Dictionary<TKeyInner, TValue>(innerDict));
		}

		public void CopyToStatic() {
			foreach (var (key, innerDict) in Value)
				_staticDictionary[key] = new Dictionary<TKeyInner, TValue>(innerDict);
		}

		#region IDictionary<TKeyOuter, Dictionary<TKeyInner, TValue>>
		public Dictionary<TKeyInner, TValue> this[TKeyOuter key] { get => Value[key]; set => Value[key] = value; }

		public ICollection<TKeyOuter> Keys => Value.Keys;

		public ICollection<Dictionary<TKeyInner, TValue>> Values => Value.Values;

		public int Count => Value.Count;

		public bool IsReadOnly => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).IsReadOnly;

		public void Add(TKeyOuter key, Dictionary<TKeyInner, TValue> value) => Value.Add(key, value);

		void ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.Add(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>> item) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).Add(item);

		public void Clear() => Value.Clear();

		bool ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.Contains(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>> item) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).Contains(item);

		public bool ContainsKey(TKeyOuter key) => Value.ContainsKey(key);

		void ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.CopyTo(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).CopyTo(array, arrayIndex);

		public Dictionary<TKeyOuter, Dictionary<TKeyInner, TValue>>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>> IEnumerable<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool Remove(TKeyOuter key) => Value.Remove(key);

		bool ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>.Remove(KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>> item) => ((ICollection<KeyValuePair<TKeyOuter, Dictionary<TKeyInner, TValue>>>)Value).Remove(item);

		public bool TryGetValue(TKeyOuter key, out Dictionary<TKeyInner, TValue> value) => Value.TryGetValue(key, out value);
		#endregion
	}
}
