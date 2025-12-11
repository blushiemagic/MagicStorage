using System;
using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	public class ArrayProvider<T>(T[] staticArray) : IReadOnlyValueProvider<T[]>, IList<T> {
		private readonly T[] _staticArray = staticArray;

		public T[] Value { get; } = [];

		public void ClearStatic() => Array.Clear(_staticArray);

		public void CopyFromStatic() {
			Array.Clear(Value);
			Array.Copy(_staticArray, Value, Math.Min(_staticArray.Length, Value.Length));
		}

		public void CopyToStatic() => Array.Copy(Value, _staticArray, Math.Min(_staticArray.Length, Value.Length));

		#region IList<T>
		// Unlike the other collection providers, the bodies of these methods will not be changed.
		// This is to allow SZArrayHelper to correctly replace the method stub with the appropriate generic method.

		public T this[int index] { get => Value[index]; set => Value[index] = value; }

		public int Count => ((ICollection<T>)Value).Count;

		public bool IsReadOnly => ((ICollection<T>)Value).IsReadOnly;

		public void Add(T item) => ((ICollection<T>)Value).Add(item);

		public void Clear() => ((ICollection<T>)Value).Clear();

		public bool Contains(T item) => ((ICollection<T>)Value).Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)Value).CopyTo(array, arrayIndex);

		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Value).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();

		public int IndexOf(T item) => ((IList<T>)Value).IndexOf(item);

		public void Insert(int index, T item) => ((IList<T>)Value).Insert(index, item);

		public bool Remove(T item) => ((ICollection<T>)Value).Remove(item);

		public void RemoveAt(int index) => ((IList<T>)Value).RemoveAt(index);
		#endregion
	}
}
