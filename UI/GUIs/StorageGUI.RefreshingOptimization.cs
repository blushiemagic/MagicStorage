using MagicStorage.Common.Systems;
using System.Collections.Generic;
using System.Linq;

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
			
			if (itemTypesToUpdate.Add(itemType))
				NetHelper.Report(true, $"Setting next refresh to check {itemTypesToUpdate.Count} items");
		}

		/// <summary>
		/// Adds <paramref name="itemTypes"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemTypes">An enumeration of item IDs to refresh.  If <see langword="null"/> or empty, then nothing happens</param>
		public static void SetNextItemTypesToRefresh(IEnumerable<int> itemTypes) {
			if (itemTypes is null || !itemTypes.Any())
				return;

			itemTypesToUpdate ??= new();
			
			#if NETPLAY
			bool any = false;
			foreach (int id in itemTypes)
				any |= itemTypesToUpdate.Add(id);
			
			if (any)
				NetHelper.Report(true, $"Setting next refresh to check {itemTypesToUpdate.Count} items");
			#else
			foreach (int id in itemTypes)
				itemTypesToUpdate.Add(id);
			#endif
		}

		/// <summary>
		/// Adds <paramref name="itemTypes"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemTypes">An array of item IDs to refresh.  If <see langword="null"/> or empty, then nothing happens</param>
		public static void SetNextItemTypesToRefresh(params int[] itemTypes) => SetNextItemTypesToRefresh((IEnumerable<int>)itemTypes);
	}
}
