using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Thread-local set wrapper that can copy values to and from a shared static set.
	/// </summary>
	public class HashSetProvider<T>(HashSet<T> staticSet) : IReadOnlyValueProvider<HashSet<T>>, ISet<T> {
		private readonly HashSet<T> _staticSet = staticSet;

		/// <summary>
		/// The shared set backing this provider.
		/// </summary>
		public HashSet<T> StaticSource => _staticSet;

		/// <summary>
		/// The local set used by the current provider instance.
		/// </summary>
		public HashSet<T> Value { get; } = [];

		/// <summary>
		/// Clears the shared static set.
		/// </summary>
		public void ClearStatic() => _staticSet.Clear();

		/// <summary>
		/// Copies all shared static values into the local set.
		/// </summary>
		public void CopyFromStatic() {
			Value.Clear();
			foreach (var item in _staticSet)
				Value.Add(item);
		}

		/// <summary>
		/// Adds all local values to the shared static set.
		/// </summary>
		public void CopyToStatic() {
			foreach (var item in Value)
				_staticSet.Add(item);
		}

		#region ISet<T>
		/// <inheritdoc/>
		public int Count => Value.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => ((ICollection<T>)Value).IsReadOnly;

		/// <inheritdoc/>
		public bool Add(T item) => Value.Add(item);

		void ICollection<T>.Add(T item) => Value.Add(item);

		/// <inheritdoc/>
		public void Clear() => Value.Clear();

		/// <inheritdoc/>
		public bool Contains(T item) => Value.Contains(item);

		/// <inheritdoc/>
		public void CopyTo(T[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

		/// <inheritdoc/>
		public void ExceptWith(IEnumerable<T> other) => Value.ExceptWith(other);

		/// <inheritdoc/>
		public HashSet<T>.Enumerator GetEnumerator() => Value.GetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc/>
		public void IntersectWith(IEnumerable<T> other) => Value.IntersectWith(other);

		/// <inheritdoc/>
		public bool IsProperSubsetOf(IEnumerable<T> other) => Value.IsProperSubsetOf(other);

		/// <inheritdoc/>
		public bool IsProperSupersetOf(IEnumerable<T> other) => Value.IsProperSupersetOf(other);

		/// <inheritdoc/>
		public bool IsSubsetOf(IEnumerable<T> other) => Value.IsSubsetOf(other);

		/// <inheritdoc/>
		public bool IsSupersetOf(IEnumerable<T> other) => Value.IsSupersetOf(other);

		/// <inheritdoc/>
		public bool Overlaps(IEnumerable<T> other) => Value.Overlaps(other);

		/// <inheritdoc/>
		public bool Remove(T item) => Value.Remove(item);

		/// <inheritdoc/>
		public bool SetEquals(IEnumerable<T> other) => Value.SetEquals(other);

		/// <inheritdoc/>
		public void SymmetricExceptWith(IEnumerable<T> other) => Value.SymmetricExceptWith(other);

		/// <inheritdoc/>
		public void UnionWith(IEnumerable<T> other) => Value.UnionWith(other);
		#endregion
	}
}
