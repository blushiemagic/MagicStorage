using System;
using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	public class TupleListProvider<T1, T2> : IEnumerable<GenericRecord<T1, T2>> {
		private readonly ListProvider<T1> _provider1;
		private readonly ListProvider<T2> _provider2;

		public List<GenericRecord<T1, T2>> List { get; set; } = [];

		public TupleListProvider(ListProvider<T1> provider1, ListProvider<T2> provider2) {
			_provider1 = provider1;
			_provider2 = provider2;

			int count = Math.Min(provider1.Count, provider2.Count);
			for (int i = 0; i < count; i++)
				List.Add(new GenericRecord<T1, T2>(provider1[i], provider2[i]));
		}

		public void CopyToProviders() {
			_provider1.Clear();
			_provider2.Clear();

			foreach (var (item1, item2) in List) {
				_provider1.Add(item1);
				_provider2.Add(item2);
			}
		}

		public Func<GenericRecord<T1, T2>, TOut> WrapFunction<TOut>(Func<T1, TOut> function) => new Wrapper1<TOut>(function).Unwrap;

		private record class Wrapper1<TOut>(Func<T1, TOut> Function) {
			public TOut Unwrap(GenericRecord<T1, T2> @record) => Function(@record.Item1);
		}

		public Func<GenericRecord<T1, T2>, TOut> WrapFunction<TOut>(Func<T2, TOut> function) => new Wrapper2<TOut>(function).Unwrap;

		private record class Wrapper2<TOut>(Func<T2, TOut> Function) {
			public TOut Unwrap(GenericRecord<T1, T2> @record) => Function(@record.Item2);
		}

		public void Add(T1 item1, T2 item2) => List.Add(new GenericRecord<T1, T2>(item1, item2));

		public void AddRange(IEnumerable<T1> items1, IEnumerable<T2> items2) {
			var enumerator1 = items1.GetEnumerator();
			var enumerator2 = items2.GetEnumerator();

			while (enumerator1.MoveNext() && enumerator2.MoveNext())
				List.Add(new GenericRecord<T1, T2>(enumerator1.Current, enumerator2.Current));
		}

		public bool Contains(T1 item) => Contains(item, EqualityComparer<T1>.Default);

		public bool Contains(T1 item, IEqualityComparer<T1> comparer) {
			foreach (var items in List) {
				if (comparer.Equals(items.Item1, item))
					return true;
			}

			return false;
		}

		public bool Contains(T2 item) => Contains(item, EqualityComparer<T2>.Default);

		public bool Contains(T2 item, IEqualityComparer<T2> comparer) {
			foreach (var items in List) {
				if (comparer.Equals(items.Item2, item))
					return true;
			}

			return false;
		}

		public bool Contains(T1 item1, T2 item2) => Contains(item1, EqualityComparer<T1>.Default, item2, EqualityComparer<T2>.Default);

		public bool Contains(T1 item1, IEqualityComparer<T1> comparer1, T2 item2, IEqualityComparer<T2> comparer2) {
			foreach (var items in List) {
				if (comparer1.Equals(items.Item1, item1) && comparer2.Equals(items.Item2, item2))
					return true;
			}
			return false;
		}

		public int IndexOf(T1 item) => IndexOf(item, EqualityComparer<T1>.Default);

		public int IndexOf(T1 item, IEqualityComparer<T1> comparer) {
			for (int i = 0; i < List.Count; i++) {
				if (comparer.Equals(List[i].Item1, item))
					return i;
			}

			return -1;
		}

		public int IndexOf(T2 item) => IndexOf(item, EqualityComparer<T2>.Default);

		public int IndexOf(T2 item, IEqualityComparer<T2> comparer) {
			for (int i = 0; i < List.Count; i++) {
				if (comparer.Equals(List[i].Item2, item))
					return i;
			}
			return -1;
		}

		public int IndexOf(T1 item1, T2 item2) => IndexOf(item1, EqualityComparer<T1>.Default, item2, EqualityComparer<T2>.Default);

		public int IndexOf(T1 item1, IEqualityComparer<T1> comparer1, T2 item2, IEqualityComparer<T2> comparer2) {
			for (int i = 0; i < List.Count; i++) {
				if (comparer1.Equals(List[i].Item1, item1) && comparer2.Equals(List[i].Item2, item2))
					return i;
			}

			return -1;
		}

		public void Insert(int index, T1 item1, T2 item2) => List.Insert(index, new GenericRecord<T1, T2>(item1, item2));

		public bool Remove(T1 item) => Remove(item, EqualityComparer<T1>.Default);

		public bool Remove(T1 item, IEqualityComparer<T1> comparer) {
			for (int i = 0; i < List.Count; i++) {
				if (comparer.Equals(List[i].Item1, item)) {
					List.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		public bool Remove(T2 item) => Remove(item, EqualityComparer<T2>.Default);

		public bool Remove(T2 item, IEqualityComparer<T2> comparer) {
			for (int i = 0; i < List.Count; i++) {
				if (comparer.Equals(List[i].Item2, item)) {
					List.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		public bool Remove(T1 item1, T2 item2) => Remove(item1, EqualityComparer<T1>.Default, item2, EqualityComparer<T2>.Default);

		public bool Remove(T1 item1, IEqualityComparer<T1> comparer1, T2 item2, IEqualityComparer<T2> comparer2) {
			for (int i = 0; i < List.Count; i++) {
				var value = List[i];
				if (comparer1.Equals(value.Item1, item1) && comparer2.Equals(value.Item2, item2)) {
					List.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		#region IEnumerable
		public List<GenericRecord<T1, T2>>.Enumerator GetEnumerator() => List.GetEnumerator();

		IEnumerator<GenericRecord<T1, T2>> IEnumerable<GenericRecord<T1, T2>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		#endregion
	}
}
