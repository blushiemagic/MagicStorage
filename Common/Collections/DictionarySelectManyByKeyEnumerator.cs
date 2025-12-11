using SerousCommonLib.API.Iterators;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MagicStorage.Common.Collections {
	// Equivalent to:  enumerable.Where(dictionary.ContainsKey).SelectMany(x => dictionary[x])
	internal class DictionarySelectManyByKeyEnumerator<TKey, TCollection, TValue> : Iterator<TValue>
		where TCollection : IEnumerable<TValue>
	{
		private readonly IEnumerable<TKey> _source;
		private readonly Dictionary<TKey, TCollection> _dictionary;
		private IEnumerator<TKey> _sourceEnumerator;
		private IEnumerator<TValue> _subEnumerator;

		public DictionarySelectManyByKeyEnumerator(IEnumerable<TKey> source, Dictionary<TKey, TCollection> dictionary) {
			_source = source;
			_dictionary = dictionary;
		}

		public override Iterator<TValue> Clone() => new DictionarySelectManyByKeyEnumerator<TKey, TCollection, TValue>(_source, _dictionary);

		public override void Dispose() {
			_sourceEnumerator?.Dispose();
			_sourceEnumerator = null;

			_subEnumerator?.Dispose();
			_subEnumerator = null;

			base.Dispose();
		}

		public override bool MoveNext() {
			switch (base._state) {
				case 1:
					// Retrieve the source enumerator
					_sourceEnumerator = _source.GetEnumerator();
					base._state = 2;
					goto case 2;
				case 2:
					Debug.Assert(_sourceEnumerator is not null);

					// Iterate until a key exists with values
					while (_sourceEnumerator.MoveNext()) {
						if (_dictionary.TryGetValue(_sourceEnumerator.Current, out var enumerable)) {
							// Fast path: empty collections can be skipped
							if (enumerable.TryGetNonEnumeratedCount(out int count) && count == 0)
								continue;

							// Slow path: get the enumerator for the current key's values
							_subEnumerator = enumerable.GetEnumerator();
							base._state = 3;
							goto case 3;
						}
					}

					// No more keys
					break;
				case 3:
					Debug.Assert(_subEnumerator is not null);

					// Enumerate the new key's values
					if (!_subEnumerator.MoveNext()) {
						_subEnumerator.Dispose();
						_subEnumerator = null;
						base._state = 2;
						goto case 2;
					}

					base._current = _subEnumerator.Current;
					return true;
			}

			Dispose();
			return false;
		}
	}
}
