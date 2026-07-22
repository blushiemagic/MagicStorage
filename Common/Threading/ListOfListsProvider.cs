using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Mirrors a static list of lists into a mutable working list.
	/// </summary>
	/// <typeparam name="T">The element type contained by each nested list.</typeparam>
	/// <param name="staticList">The static list mirrored by this provider.</param>
	public class ListOfListsProvider<T>(List<List<T>> staticList) : IReadOnlyValueProvider<List<List<T>>>, IList<List<T>> {
		private readonly List<List<T>> _staticList = staticList;

		/// <inheritdoc />
		public List<List<T>> StaticSource => _staticList;

		/// <inheritdoc />
		public List<List<T>> Value { get; } = [];

		/// <inheritdoc />
		public void ClearStatic() => _staticList.Clear();

		/// <inheritdoc />
		public void CopyFromStatic() {
			Value.Clear();

			foreach (var list in _staticList)
				Value.Add([.. list]);
		}

		/// <inheritdoc />
		public void CopyToStatic() => _staticList.AddRange(Value);

		/// <summary>
		/// Appends the specified nested lists to the working list.
		/// </summary>
		/// <param name="items">The nested lists to append.</param>
		public void AddRange(IEnumerable<List<T>> items) => Value.AddRange(items);

		#region IList<List<T>>
		/// <inheritdoc />
		public List<T> this[int index] { get => Value[index]; set => Value[index] = value; }

		/// <inheritdoc />
		public int Count => Value.Count;

		/// <inheritdoc />
		public bool IsReadOnly => ((ICollection<List<T>>)Value).IsReadOnly;

		/// <inheritdoc />
		public void Add(List<T> item) => Value.Add(item);

		/// <inheritdoc />
		public void Clear() => Value.Clear();

		/// <inheritdoc />
		public bool Contains(List<T> item) => Value.Contains(item);

		/// <inheritdoc />
		public void CopyTo(List<T>[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public List<List<T>>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<List<T>> IEnumerable<List<T>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		
		/// <inheritdoc />
		public int IndexOf(List<T> item) => Value.IndexOf(item);
		
		/// <inheritdoc />
		public void Insert(int index, List<T> item) => Value.Insert(index, item);
		
		/// <inheritdoc />
		public bool Remove(List<T> item) => Value.Remove(item);
		
		/// <inheritdoc />
		public void RemoveAt(int index) => Value.RemoveAt(index);
		#endregion
	}
}
