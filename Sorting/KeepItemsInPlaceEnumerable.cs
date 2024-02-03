using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

namespace MagicStorage.Sorting {
	internal class KeepItemsInPlaceEnumerable<T> : IOrderedEnumerable<T> {
		private readonly IEnumerable<T> source;

		public KeepItemsInPlaceEnumerable(IEnumerable<T> source) {
			this.source = source;
		}

		public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending) => this;

		public IEnumerator<T> GetEnumerator() => source.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
