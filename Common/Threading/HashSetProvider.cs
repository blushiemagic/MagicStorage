using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	public class HashSetProvider<T>(HashSet<T> staticSet) : IReadOnlyValueProvider<HashSet<T>>, ISet<T> {
		private readonly HashSet<T> _staticSet = staticSet;

		public HashSet<T> StaticSource => _staticSet;

		public HashSet<T> Value { get; } = [];

		public void ClearStatic() => _staticSet.Clear();

		public void CopyFromStatic() {
			Value.Clear();
			foreach (var item in _staticSet)
				Value.Add(item);
		}

		public void CopyToStatic() {
			foreach (var item in Value)
				_staticSet.Add(item);
		}

		#region ISet<T>
		public int Count => Value.Count;

		public bool IsReadOnly => ((ICollection<T>)Value).IsReadOnly;

		public bool Add(T item) => Value.Add(item);

		void ICollection<T>.Add(T item) => Value.Add(item);

		public void Clear() => Value.Clear();

		public bool Contains(T item) => Value.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

		public void ExceptWith(IEnumerable<T> other) => Value.ExceptWith(other);

		public HashSet<T>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void IntersectWith(IEnumerable<T> other) => Value.IntersectWith(other);

		public bool IsProperSubsetOf(IEnumerable<T> other) => Value.IsProperSubsetOf(other);

		public bool IsProperSupersetOf(IEnumerable<T> other) => Value.IsProperSupersetOf(other);

		public bool IsSubsetOf(IEnumerable<T> other) => Value.IsSubsetOf(other);

		public bool IsSupersetOf(IEnumerable<T> other) => Value.IsSupersetOf(other);

		public bool Overlaps(IEnumerable<T> other) => Value.Overlaps(other);

		public bool Remove(T item) => Value.Remove(item);

		public bool SetEquals(IEnumerable<T> other) => Value.SetEquals(other);

		public void SymmetricExceptWith(IEnumerable<T> other) => Value.SymmetricExceptWith(other);

		public void UnionWith(IEnumerable<T> other) => Value.UnionWith(other);
		#endregion
	}
}
