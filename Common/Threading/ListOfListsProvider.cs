using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	public class ListOfListsProvider<T>(List<List<T>> staticList) : IReadOnlyValueProvider<List<List<T>>>, IList<List<T>> {
		private readonly List<List<T>> _staticList = staticList;

		public List<List<T>> Value { get; } = [];

		public void ClearStatic() => _staticList.Clear();

		public void CopyFromStatic() {
			Value.Clear();

			foreach (var list in _staticList)
				Value.Add([.. list]);
		}

		public void CopyToStatic() => _staticList.AddRange(Value);

		public void AddRange(IEnumerable<List<T>> items) => Value.AddRange(items);

		#region IList<List<T>>
		public List<T> this[int index] { get => Value[index]; set => Value[index] = value; }

		public int Count => Value.Count;

		public bool IsReadOnly => ((ICollection<List<T>>)Value).IsReadOnly;

		public void Add(List<T> item) => Value.Add(item);

		public void Clear() => Value.Clear();

		public bool Contains(List<T> item) => Value.Contains(item);

		public void CopyTo(List<T>[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

		public List<List<T>>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<List<T>> IEnumerable<List<T>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		
		public int IndexOf(List<T> item) => Value.IndexOf(item);
		
		public void Insert(int index, List<T> item) => Value.Insert(index, item);
		
		public bool Remove(List<T> item) => Value.Remove(item);
		
		public void RemoveAt(int index) => Value.RemoveAt(index);
		#endregion
	}
}
