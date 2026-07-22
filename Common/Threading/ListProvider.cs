using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Thread-local list wrapper that can copy values to and from a shared static list.
	/// </summary>
	public class ListProvider<T>(List<T> staticList) : IReadOnlyValueProvider<List<T>>, IList<T> {
		private readonly List<T> _staticList = staticList;

		/// <summary>
		/// The shared list backing this provider.
		/// </summary>
		public List<T> StaticSource => _staticList;

		/// <summary>
		/// The local list used by the current provider instance.
		/// </summary>
		public List<T> Value { get; } = [];

		/// <summary>
		/// Clears the shared static list.
		/// </summary>
		public void ClearStatic() => _staticList.Clear();

		/// <summary>
		/// Copies all shared static values into the local list.
		/// </summary>
		public void CopyFromStatic() {
			Value.Clear();
			Value.AddRange(_staticList);
		}

		/// <summary>
		/// Appends all local values to the shared static list.
		/// </summary>
		public void CopyToStatic() => _staticList.AddRange(Value);

		/// <summary>
		/// Appends a range of values to the local list.
		/// </summary>
		public void AddRange(IEnumerable<T> items) => Value.AddRange(items);

		#region IList<T>
		/// <inheritdoc/>
		public T this[int index] { get => Value[index]; set => Value[index] = value; }

		/// <inheritdoc/>
		public int Count => Value.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => ((ICollection<T>)Value).IsReadOnly;

		/// <inheritdoc/>
		public void Add(T item) => Value.Add(item);

		/// <inheritdoc/>
		public void Clear() => Value.Clear();

		/// <inheritdoc/>
		public bool Contains(T item) => Value.Contains(item);

		/// <inheritdoc/>
		public void CopyTo(T[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

		/// <inheritdoc/>
		public List<T>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc/>
		public int IndexOf(T item) => Value.IndexOf(item);

		/// <inheritdoc/>
		public void Insert(int index, T item) => Value.Insert(index, item);

		/// <inheritdoc/>
		public bool Remove(T item) => Value.Remove(item);

		/// <inheritdoc/>
		public void RemoveAt(int index) => Value.RemoveAt(index);
		#endregion
	}
}
