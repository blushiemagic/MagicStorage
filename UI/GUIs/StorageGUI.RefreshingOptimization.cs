using MagicStorage.Common.Systems;
using System.Collections.Generic;

namespace MagicStorage {
	partial class StorageGUI {
		// Specialized collection for making only certain item types get recalculated
		private static HashSet<int> itemTypesToUpdate;

		/// <summary>
		/// Adds <paramref name="itemType"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemType">An item ID to refresh</param>
		public static void SetNextItemTypeToRefresh(int itemType) {
			itemTypesToUpdate ??= new();
			itemTypesToUpdate.Add(itemType);

			NetHelper.Report(true, $"Setting next refresh to check {itemTypesToUpdate.Count} items");
		}

		/// <summary>
		/// Adds <paramref name="itemTypes"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemTypes">An enumeration of item IDs to refresh.  If <see langword="null"/> or empty, then nothing happens</param>
		public static void SetNextItemTypesToRefresh(IEnumerable<int> itemTypes) {
			if (itemTypes is null)
				return;

			itemTypesToUpdate ??= new();
			
			foreach (int id in itemTypes)
				itemTypesToUpdate.Add(id);

			if (itemTypesToUpdate.Count == 0)
				itemTypesToUpdate = null;
			else
				NetHelper.Report(true, $"Setting next refresh to check {itemTypesToUpdate.Count} items");
		}

		/// <summary>
		/// Adds <paramref name="itemTypes"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemTypes">An array of item IDs to refresh.  If <see langword="null"/> or empty, then nothing happens</param>
		public static void SetNextItemTypesToRefresh(params int[] itemTypes) => SetNextItemTypesToRefresh((IEnumerable<int>)itemTypes);
	}
}
