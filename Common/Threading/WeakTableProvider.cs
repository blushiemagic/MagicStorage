using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Mirrors a static <see cref="ConditionalWeakTable{TKey, TValue}" /> into a mutable working table.
	/// </summary>
	/// <typeparam name="TKey">The reference key type.</typeparam>
	/// <typeparam name="TValue">The reference value type.</typeparam>
	/// <param name="staticTable">The static weak table mirrored by this provider.</param>
	public class WeakTableProvider<TKey, TValue>(ConditionalWeakTable<TKey, TValue> staticTable) : IReadOnlyValueProvider<ConditionalWeakTable<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
		where TKey : class
		where TValue : class
	{
		private readonly ConditionalWeakTable<TKey, TValue> _staticTable = staticTable;

		/// <inheritdoc />
		public ConditionalWeakTable<TKey, TValue> StaticSource => _staticTable;

		/// <inheritdoc />
		public ConditionalWeakTable<TKey, TValue> Value { get; } = [];

		/// <inheritdoc />
		public void ClearStatic() => _staticTable.Clear();

		/// <inheritdoc />
		public void CopyFromStatic() {
			Value.Clear();
			foreach (var (key, value) in _staticTable)
				Value.Add(key, value);
		}

		/// <inheritdoc />
		public void CopyToStatic() {
			foreach (var (key, value) in Value)
				_staticTable.AddOrUpdate(key, value);
		}

		#region ConditionalWeakTable<TKey, TValue> mirrors
		/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.TryGetValue(TKey, out TValue)" />
		public bool TryGetValue(TKey key, out TValue value) => Value.TryGetValue(key, out value);

		/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.Add(TKey, TValue)" />
		public void Add(TKey key, TValue value) => Value.Add(key, value);

		/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.TryAdd(TKey, TValue)" />
		public bool TryAdd(TKey key, TValue value) => Value.TryAdd(key, value);

		/// <summary>
		/// Adds a value for the specified key or replaces the existing value.
		/// </summary>
		/// <param name="key">The key to update.</param>
		/// <param name="value">The value to store.</param>
		public void AddOrUpdate(TKey key, TValue value) => Value.AddOrUpdate(key, value);

		/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.Remove(TKey)" />
		public bool Remove(TKey key) => Value.Remove(key);

		/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.Clear()" />
		public void Clear() => Value.Clear();

		/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.GetValue(TKey, ConditionalWeakTable{TKey, TValue}.CreateValueCallback)" />
		public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback) => Value.GetValue(key, createValueCallback);

		/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.GetOrCreateValue(TKey)" />
		public TValue GetOrCreateValue(TKey key) => Value.GetOrCreateValue(key);

		/// <inheritdoc />
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Value.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		#endregion
	}

	/// <summary>
	/// Extension helpers for <see cref="WeakTableProvider{TKey, TValue}" />.
	/// </summary>
	public static class WeakTableProviderExtensions {
		/// <summary>
		/// Checks whether the provider's working table contains the specified key.
		/// </summary>
		/// <typeparam name="TKey">The reference key type.</typeparam>
		/// <typeparam name="TValue">The reference value type.</typeparam>
		/// <param name="this">The provider to inspect.</param>
		/// <param name="key">The key to search for.</param>
		/// <returns><see langword="true" /> if the key exists; otherwise, <see langword="false" />.</returns>
		public static bool ContainsKey<TKey, TValue>(this WeakTableProvider<TKey, TValue> @this, TKey key)
			where TKey : class
			where TValue : class
		{
			return @this.Value.TryGetValue(key, out _);
		}
	}
}
