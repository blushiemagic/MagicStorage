using System;
using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Thread-local array wrapper that can copy values to and from a shared static array.
	/// </summary>
	public class ArrayProvider<T>(T[] staticArray) : IReadOnlyValueProvider<T[]>, IList<T> {
		private readonly T[] _staticArray = staticArray;

		/// <summary>
		/// The shared array backing this provider.
		/// </summary>
		public T[] StaticSource => _staticArray;

		/// <summary>
		/// The local array used by the current provider instance.
		/// </summary>
		public T[] Value { get; } = [];

		/// <summary>
		/// Clears the shared static array.
		/// </summary>
		public void ClearStatic() => Array.Clear(_staticArray);

		/// <summary>
		/// Copies shared static values into the local array up to the shorter array length.
		/// </summary>
		public void CopyFromStatic() {
			Array.Clear(Value);
			Array.Copy(_staticArray, Value, Math.Min(_staticArray.Length, Value.Length));
		}

		/// <summary>
		/// Copies local values into the shared static array up to the shorter array length.
		/// </summary>
		public void CopyToStatic() => Array.Copy(Value, _staticArray, Math.Min(_staticArray.Length, Value.Length));

		#region IList<T>
		// Unlike the other collection providers, the bodies of these methods will not be changed.
		// This is to allow SZArrayHelper to correctly replace the method stub with the appropriate generic method.

		/// <inheritdoc/>
		public T this[int index] { get => Value[index]; set => Value[index] = value; }

		/// <inheritdoc/>
		public int Count => ((ICollection<T>)Value).Count;

		/// <inheritdoc/>
		public bool IsReadOnly => ((ICollection<T>)Value).IsReadOnly;

		/// <inheritdoc/>
		public void Add(T item) => ((ICollection<T>)Value).Add(item);

		/// <inheritdoc/>
		public void Clear() => ((ICollection<T>)Value).Clear();

		/// <inheritdoc/>
		public bool Contains(T item) => ((ICollection<T>)Value).Contains(item);

		/// <inheritdoc/>
		public void CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)Value).CopyTo(array, arrayIndex);

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Value).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();

		/// <inheritdoc/>
		public int IndexOf(T item) => ((IList<T>)Value).IndexOf(item);

		/// <inheritdoc/>
		public void Insert(int index, T item) => ((IList<T>)Value).Insert(index, item);

		/// <inheritdoc/>
		public bool Remove(T item) => ((ICollection<T>)Value).Remove(item);

		/// <inheritdoc/>
		public void RemoveAt(int index) => ((IList<T>)Value).RemoveAt(index);
		#endregion
	}
}
