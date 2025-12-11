using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IStorageItemsPovider {
		StorageItems StorageItems { get; }
	}

	public class StorageItems {
		public readonly List<Item> allStoredItems = [];

		public void CollectObjects(RefreshThread thread) {
			allStoredItems.Clear();
			allStoredItems.AddRange(thread.Heart.GetStoredItems());
		}
	}
}
