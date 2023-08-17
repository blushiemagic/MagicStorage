using System.Collections.Generic;

namespace MagicStorage {
	partial class StorageGUI {
		// Specialized collection for making only certain item types get recalculated
		private static HashSet<int> itemTypesToUpdate;
		private static bool forceFullRefresh;

		public static void SetNextItemTypeToRefresh(int itemType) {
			itemTypesToUpdate ??= new();
			itemTypesToUpdate.Add(itemType);
		}

		public static void SetNextItemTypesToRefresh(IEnumerable<int> itemTypes) {
			itemTypesToUpdate ??= new();
			
			foreach (int id in itemTypes)
				itemTypesToUpdate.Add(id);
		}
	}
}
