using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.UI.States;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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

		private class FullRefreshBuilder : IRefreshThreadBuilder {
			public static IRefreshThreadBuilder Instance { get; } = new FullRefreshBuilder();

			public StorageViewControls CreateControls() => CraftingGUI.CreateRefreshThreadControls(MagicUI.decraftingUI);

			public RefreshThread CreateThread(StorageViewControls controls) {
				// Force all items to be recalculated
				if (MagicUI.ForceNextRefreshToBeFull)
					itemsToRefresh = null;

				return new ShimmeringRefreshThread(
					controls: controls,
					processedStorage: new(
						staticWasModuleItemTable: CraftingGUI.wasModuleItem,
						staticModuleItemWasFromInventoryTable: CraftingGUI.moduleItemWasFromInventory,
						staticResultItemsList: CraftingGUI.items,
						staticResultItemGroupsList: CraftingGUI.itemGroups,
						staticResultItemsFromModulesList: CraftingGUI.sourceItemsFromModules,
						staticCountsDictionary: CraftingGUI.itemCounts,
						staticCountsByPrefixDictionary: CraftingGUI.itemCountsByPrefix
					),
					mainZoneControls: new(
						zoneObjectFilterChoice: MagicUI.decraftingUI.GetDefaultPage<DecraftingUIState.ShimmeringPage>().recipeButtons.Choice,
						favorited: StoragePlayer.LocalPlayer.FavoritedShimmerItems,
						hidden: StoragePlayer.LocalPlayer.HiddenShimmerItems,
						configBlacklist: MagicStorageConfig.GlobalShimmerItemBlacklist
					),
					mainZoneResults: new(
						objectsToRefresh: itemsToRefresh,
						staticObjectList: viewingItems,
						staticAvailableList: itemAvailable
					),
					ingredientControls: new(
						staticShowAllIngredientsField: new ConstantValueProvider<bool>(false),
						staticInfiniteItemsSet: CraftingGUI.isItemInfinite,
						staticBlockedList:  CraftingGUI.blockStorageItems,
						staticCreativeUnitField: new CraftingGUI.CreativeUnitPresentProvider()
					),
					craftingObject: new(
						selection: new SelectionProvider(),
						craftAmountTarget: new CraftingGUI.CraftAmountTargetProvider()
					),
					staticReportCacheList: cachedShimmerReports
				);
			}
		}

		private class SelectionProvider : IReadOnlyValueProvider<int> {
			public int Value { get; private set; }
			public SelectionProvider() => CopyFromStatic();
			public SelectionProvider(int defaultValue) => Value = defaultValue;
			public void ClearStatic() { }
			public void CopyFromStatic() => Value = selectedItem;
			public void CopyToStatic() => selectedItem = Value;
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
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, ICraftingObjectProvider<int>, IShimmerItemReportsProvider
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

			var handler = new ZoneResultItemsHandler<T>(
				thread: thread,
				staticStoredIngredientsList: CraftingGUI.storageItems,
				staticStoredIngredientsInfoList: CraftingGUI.storageItemInfo,
				staticResultItemsList: resultItems,
				staticResultItemsInfoList: resultItemsInfo
			);

			thread.IngredientControls.recipeItemsHandler = handler;

			var reports = thread.ShimmerItemReports.reports.Value;

			foreach (var items in resultItemGroups.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				foreach (Item item in items) {
					CraftingGUI.CheckItemFromSource(handler, item, selection, IsItemValidForStorage);

					// Reduce hot path execution time by using args instead of the thread object directly
					if (IsItemValidForResult(item, selection, reports))
						handler.SetResultItem(item);
				}
			}

			handler.CompactCollections();

			NetHelper.Report(true, $"Success! Found {handler.StoredIngredientCount} items and {(handler.FoundStoredResultItem ? "no" : $"{handler.resultItems.Count}")} result items");
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
