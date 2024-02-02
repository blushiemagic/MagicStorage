using MagicStorage.Common.Systems;
using System.Collections.Generic;
using System.Linq;

namespace MagicStorage {
	partial class DecraftingGUI {
		internal static HashSet<int> itemsToRefresh;
		
		/// <summary>
		/// Adds <paramref name="itemType"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemType">An item ID to refresh</param>
		public static void SetNextDefaultItemCollectionToRefresh(int itemType) {
			itemsToRefresh ??= new();
			itemsToRefresh.Add(itemType);

			NetHelper.Report(true, $"Setting next refresh to check {itemsToRefresh.Count} items");
		}

		/// <summary>
		/// Adds <paramref name="itemTypes"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemTypes">An enumeration of item IDs to refresh.  If <see langword="null"/> or empty, then nothing happens</param>
		public static void SetNextDefaultItemCollectionToRefresh(IEnumerable<int> itemTypes) {
			if (itemTypes is null)
				return;

			itemsToRefresh ??= new();
			
			foreach (int id in itemTypes)
				itemsToRefresh.Add(id);

			if (itemsToRefresh.Count == 0)
				itemsToRefresh = null;
			else
				NetHelper.Report(true, $"Setting next refresh to check {itemsToRefresh.Count} items");
		}

		/// <summary>
		/// Adds <paramref name="itemTypes"/> to the list of items to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="itemTypes">An array of item IDs to refresh.  If <see langword="null"/>, then nothing happens</param>
		public static void SetNextDefaultItemCollectionToRefresh(int[] itemTypes) => SetNextDefaultItemCollectionToRefresh((IEnumerable<int>)itemTypes);
	}
}
