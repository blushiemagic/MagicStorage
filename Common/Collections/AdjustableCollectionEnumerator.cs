using SerousCommonLib.API.Iterators;
using System;
using System.Collections.Generic;

namespace MagicStorage.Common.Collections {
	/// <summary>
	/// An enumerator over a list of objects that allows appending new items during enumeration.
	/// </summary>
	public class AdjustableCollectionEnumerator<T> : Iterator<T> {
		private readonly IEnumerable<T> _sourceEnumeration;
		private readonly int _startingIndex;
		private readonly List<T> _source;
		private int _index;

		public int Count => _source is null ? 0 : _source.Count;

		public AdjustableCollectionEnumerator(IEnumerable<T> source) : this(source, 0) { }

		public AdjustableCollectionEnumerator(IEnumerable<T> source, int startingIndex) {
			_sourceEnumeration = source;
			_startingIndex = startingIndex;
			_index = -1;
		}

		public void Append(T item) {
			_source.Add(item);
		}

		public void AppendRange(IEnumerable<T> items) {
			ThrowIfInvalidState();
			_source.AddRange(items);
		}

		public void InsertAfterCurrent(T item) {
			ThrowIfInvalidState();
			_source.Insert(_index + 1, item);
		}

		public void InsertRangeAfterCurrent(IEnumerable<T> items) {
			ThrowIfInvalidState();
			_source.InsertRange(_index + 1, items);
		}

		private void ThrowIfInvalidState() {
			ObjectDisposedException.ThrowIf(base._state < 0, instance: this);
			
			if (base._state < 2)
				throw new InvalidOperationException("The current operation is invalid because enumeration has not started.");
		}

		public override Iterator<T> Clone() => new AdjustableCollectionEnumerator<T>(_sourceEnumeration, _startingIndex);

		public override void Dispose() {
			_source.Clear();
			_index = -1;

			base.Dispose();
		}

		public override bool MoveNext() {
			switch (base._state) {
				case 1:
					if (_sourceEnumeration is null)
						break;

					_source.AddRange(_sourceEnumeration);

					if (_source.Count > _startingIndex) {
						_index = _startingIndex - 1;
						base._state = 2;
						goto case 2;
					}

					break;
				case 2:
					if (++_index < _source.Count) {
						base._current = _source[_index];
						return true;
					}

					break;
			}

			Dispose();
			return false;
		}
	}
}
