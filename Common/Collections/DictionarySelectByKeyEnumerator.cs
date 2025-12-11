using SerousCommonLib.API.Iterators;
using System.Collections.Generic;
using System.Diagnostics;

namespace MagicStorage.Common.Collections {
	// Equivalent to:  enumerable.Where(dictionary.ContainsKey).Select(x => dictionary[x])
	internal class DictionarySelectByKeyEnumerator<TKey, TValue> : Iterator<TValue> {
		private readonly IEnumerable<TKey> _source;
		private readonly Dictionary<TKey, TValue> _dictionary;
		private IEnumerator<TKey> _enumerator;

		public DictionarySelectByKeyEnumerator(IEnumerable<TKey> source, Dictionary<TKey, TValue> dictionary) {
			_source = source;
			_dictionary = dictionary;
		}

		public override Iterator<TValue> Clone() => new DictionarySelectByKeyEnumerator<TKey, TValue>(_source, _dictionary);

		public override void Dispose() {
			_enumerator?.Dispose();
			_enumerator = null;

			base.Dispose();
		}

		public override bool MoveNext() {
			switch (base._state) {
				case 1:
					_enumerator = _source.GetEnumerator();
					base._state = 2;
					goto case 2;
				case 2:
					Debug.Assert(_enumerator is not null);

					// Iterate until a key exists, or no more keys can be provided
					while (_enumerator.MoveNext()) {
						if (_dictionary.TryGetValue(_enumerator.Current, out var value)) {
							base._current = value;
							return true;
						}
					}

					Dispose();
					break;
			}

			return false;
		}
	}
}
