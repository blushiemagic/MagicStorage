using System.Collections.Generic;
using System.Linq;

namespace MagicStorage {
	partial class DecraftingGUI {
		internal static int[] itemsToRefresh;

		/// <summary>
		/// Adds <paramref name="items"/> to the list of items to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="items">An array of item IDs to refresh.  If <see langword="null"/>, then nothing happens</param>
		public static void SetNextDefaultItemCollectionToRefresh(int[] items) {
			if (itemsToRefresh is null) {
				if (items is not null)
					NetHelper.Report(true, $"Setting next refresh to check {items.Length} items");

				itemsToRefresh = items;
				return;
			}

			if (items is null)
				return;

			itemsToRefresh = itemsToRefresh.Concat(items).Distinct().ToArray();

			NetHelper.Report(true, $"Setting next refresh to check {itemsToRefresh.Length} items");
		}

		/// <summary>
		/// Adds <paramref name="item"/> to the list of items to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="item">An item ID to refresh</param>
		public static void SetNextDefaultItemCollectionToRefresh(int item) => SetNextDefaultItemCollectionToRefresh(new[] { item });

		/// <summary>
		/// Adds <paramref name="items"/> to the list of items to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="items">An enumeration of item IDs to refresh.  If <see langword="null"/>, then nothing happens</param>
		public static void SetNextDefaultItemCollectionToRefresh(IEnumerable<int> items) => SetNextDefaultItemCollectionToRefresh(items?.ToArray());
	}
}
