using System.Runtime.CompilerServices;

namespace MagicStorage {
	partial class Utility {
		public static bool ContainsKey<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> @this, TKey key)
			where TKey : class
			where TValue : class
		{
			return @this.TryGetValue(key, out _);
		}
	}
}
