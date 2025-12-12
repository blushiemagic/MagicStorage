using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MagicStorage.Common.Threading {
	public class WeakTableProvider<TKey, TValue>(ConditionalWeakTable<TKey, TValue> staticTable) : IReadOnlyValueProvider<ConditionalWeakTable<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
		where TKey : class
		where TValue : class
	{
		private readonly ConditionalWeakTable<TKey, TValue> _staticTable = staticTable;

		public ConditionalWeakTable<TKey, TValue> Value { get; } = [];

		public void ClearStatic() => _staticTable.Clear();

		public void CopyFromStatic() {
			Value.Clear();
			foreach (var (key, value) in _staticTable)
				Value.Add(key, value);
		}

		public void CopyToStatic() {
			foreach (var (key, value) in Value)
				_staticTable.AddOrUpdate(key, value);
		}

		#region ConditionalWeakTable<TKey, TValue> mirrors
		public bool TryGetValue(TKey key, out TValue value) => Value.TryGetValue(key, out value);

		public void Add(TKey key, TValue value) => Value.Add(key, value);

		public bool TryAdd(TKey key, TValue value) => Value.TryAdd(key, value);

		public void AddOrUpdate(TKey key, TValue value) => Value.AddOrUpdate(key, value);

		public bool Remove(TKey key) => Value.Remove(key);

		public void Clear() => Value.Clear();

		public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback) => Value.GetValue(key, createValueCallback);

		public TValue GetOrCreateValue(TKey key) => Value.GetOrCreateValue(key);

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Value.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		#endregion
	}

	public static class WeakTableProviderExtensions {
		public static bool ContainsKey<TKey, TValue>(this WeakTableProvider<TKey, TValue> @this, TKey key)
			where TKey : class
			where TValue : class
		{
			return @this.Value.TryGetValue(key, out _);
		}
	}
}
