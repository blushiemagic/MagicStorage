using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	public class ListProvider<T>(List<T> staticList) : IReadOnlyValueProvider<List<T>>, IList<T> {
		private readonly List<T> _staticList = staticList;

		public List<T> StaticSource => _staticList;

		public List<T> Value { get; } = [];

		public void ClearStatic() => _staticList.Clear();

		public void CopyFromStatic() {
			Value.Clear();
			Value.AddRange(_staticList);
		}

		public void CopyToStatic() => _staticList.AddRange(Value);

		public void AddRange(IEnumerable<T> items) => Value.AddRange(items);

		#region IList<T>
		public T this[int index] { get => Value[index]; set => Value[index] = value; }

		public int Count => Value.Count;

		public bool IsReadOnly => ((ICollection<T>)Value).IsReadOnly;

		public void Add(T item) => Value.Add(item);

		public void Clear() => Value.Clear();

		public bool Contains(T item) => Value.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

		public List<T>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int IndexOf(T item) => Value.IndexOf(item);

		public void Insert(int index, T item) => Value.Insert(index, item);

		public bool Remove(T item) => Value.Remove(item);

		public void RemoveAt(int index) => Value.RemoveAt(index);
		#endregion
	}
}
