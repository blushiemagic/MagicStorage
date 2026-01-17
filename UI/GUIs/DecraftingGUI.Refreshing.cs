using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace MagicStorage {
	partial class DecraftingGUI {
		public static readonly List<Item> resultItems = new();
		public static readonly List<ItemInfo> resultItemsInfo = new();

		internal static void ResetRefreshCache() {
			itemsToRefresh = null;
			CraftingGUI.hasCompleteData = false;
		}

		internal static void RefreshItems() {
			NetHelper.Report(true, "DecraftingGUI: RefreshItems invoked");

			if (itemsToRefresh is { Count: > 0 })
				NetHelper.Report(false, $"Refreshing {itemsToRefresh.Count} items...");

			CreateFullRefreshThread(caller: "DecraftingGUI.RefreshItems()").Start();

			ResetRefreshCache();
		}

		private static void AnalyzeIngredients() {
			NetHelper.Report(true, "Analyzing environment requirements...");

			CraftingGUI.ResetZoneInfo();

			CraftingGUI.AdjustAndAssignZoneInfo();
		}

		private static void SortAndFilter(ShimmeringRefreshThread thread) {
			CraftingGUI.LoadItemsAndSetDictionaryInfo(thread);
			RefreshStorageItems(thread);
			RefreshItemsAvailability(thread);
		}

		private static void RefreshStorageItems<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, ICraftingObjectProvider<int>, IShimmerItemReportsProvider, IRecipeItemsProvider
		{
			NetHelper.Report(true, "Updating stored ingredients collection and result item...");

			var selection = thread.CraftingObject.selection.Value;

			if (selection <= ItemID.None) {
				thread.InitAsCompleted("Populating Stored Ingredients");
				NetHelper.Report(true, "Failed.  No item is selected.");
				return;
			}

			var resultItemGroups = thread.ProcessedStorageItems.resultItemGroups.Value;

			thread.InitTaskSchedule(resultItemGroups.Count, "Populating Stored Ingredients");

			var handler = thread.RecipeItems;

			var reports = thread.ShimmerItemReports.reports.Value;

			foreach (var items in resultItemGroups.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				foreach (Item item in items) {
					CraftingGUI.CheckItemFromSource(handler, item, selection, IsItemValidForStorage);

					// Reduce hot path execution time by using args instead of the thread object directly
					if (IsItemValidForResult(item, selection, reports))
						handler.SetResultItem(item);
				}
			}

			handler.CompactCollections(thread);

			NetHelper.Report(true, $"Success! Found {handler.GetItemCountsReport()}");
		}

		internal static bool IsItemValidForStorage(Item item, int selectedItem) => item.type == selectedItem && item.stack > 0;

		internal static bool IsItemValidForResult(Item item) => IsItemValidForResult(item, selectedItem, MagicCache.ShimmerInfos[selectedItem].GetShimmerReports().OfType<ItemReport>());

		private static bool IsItemValidForResult(Item item, int selectedItem, IEnumerable<ItemReport> cachedShimmerReports) {
			if (selectedItem == -1)
				return false;

			IShimmerResultReport report = new ItemReport(item.type);

			foreach (var cachedReport in cachedShimmerReports) {
				if (cachedReport.Equals(report))
					return true;
			}

			return false;
		}
	}
}
