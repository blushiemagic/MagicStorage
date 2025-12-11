using System.Collections.Generic;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface ICraftStructAvailableCacheProvider<T> where T : struct {
		CraftStructAvailableCache<T> CraftStructAvailableCache { get; }
	}

	public class CraftStructAvailableCache<T> where T : struct {
		public readonly DictionaryProvider<T, bool> lookup;

		public CraftStructAvailableCache(Dictionary<T, bool> staticDictionary) {
			lookup = new DictionaryProvider<T, bool>(staticDictionary);
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
