using System.Runtime.CompilerServices;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface ICraftObjectAvailableCacheProvider<T> where T : class {
		CraftObjectAvailableCache<T> CraftObjectAvailableCache { get; }
	}

	public class CraftObjectAvailableCache<T> where T : class {
		public readonly WeakTableProvider<T, Ref<bool>> lookup;

		public CraftObjectAvailableCache(ConditionalWeakTable<T, Ref<bool>> staticTable) {
			lookup = new WeakTableProvider<T, Ref<bool>>(staticTable);
		}

		public void CopyFromStaticCollection() {
			lookup.CopyFromStatic();
		}

		public void CopyToStaticCollection() {
			lookup.OverwriteStatic();
		}

		public void ClearStaticCollection() {
			lookup.ClearStatic();
		}
	}
}
