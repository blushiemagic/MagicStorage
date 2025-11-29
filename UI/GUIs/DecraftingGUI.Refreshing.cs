using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.UI;
using MagicStorage.CrossMod;
using MagicStorage.UI.States;
using System;
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
		}

		internal static void RefreshItems() {
			if (MagicUI.ForceNextRefreshToBeFull) {
				// Force all items to be recalculated
				itemsToRefresh = null;
			}

			NetHelper.Report(true, "DecraftingGUI: RefreshItems invoked");

			CraftingGUI.GetCommonRefreshThreadParameters(out var adjTiles, out var blockedStoredIngredients, out var craftAmountTarget);

			var thread = new ShimmeringRefreshThread(
				controls: CreateRefreshThreadControls(),
				adjTiles: adjTiles,
				selectedItem: selectedItem,
				itemsToRefresh: itemsToRefresh,
				recipeFilter: MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>().recipeButtons.Choice,
				favorited: StoragePlayer.LocalPlayer.FavoritedRecipes,
				hidden: StoragePlayer.LocalPlayer.HiddenRecipes,
				configBlacklist: MagicStorageConfig.GlobalRecipeBlacklist,
				blockedStoredIngredients: blockedStoredIngredients,
				craftAmountTarget: craftAmountTarget
			);
			thread.SetDebugName("DecraftingGUI thread");
			thread.Start();

			ResetRefreshCache();
		}

		private static StorageViewControls CreateRefreshThreadControls() {
			var shimmeringPage = MagicUI.decraftingUI.GetDefaultPage<DecraftingUIState.ShimmeringPage>();

			return new StorageViewControls(
				sortingOption: SortingOptionLoader.Selected,
				filteringOption: FilteringOptionLoader.Selected,
				generalFilters: FilteringOptionLoader.GeneralSelections,
				fullSearchText: shimmeringPage.searchBar.State.InputText,
				showOnlyFavorites: MagicStorageConfig.CraftingFavoritingEnabled && shimmeringPage.recipeButtons.Choice == CraftingGUI.RecipeButtonsFavoritesChoice,
				modSearchOption: shimmeringPage.modSearchBox.ModIndex
			);
		}

		private static void PopulateShimmerSnapshots(ShimmeringRefreshThread thread) {
			IEnumerable<IShimmerResultReport> reports = thread.selectedItem == -1
				? Array.Empty<IShimmerResultReport>()
				: MagicCache.ShimmerInfos[thread.selectedItem].GetShimmerReports();

			thread.cachedShimmerReports = [.. reports.OfType<ItemReport>()];  // Ignore any reports that aren't ItemReports, since that's all the result zone cares about

			thread.decraftingRecipeAvailableSnapshot = Main.recipe.Take(Recipe.numRecipes).Select(ShimmerMetrics.IsDecraftAvailable).ToArray();
			thread.itemTypeToDecraftRecipeIndexSnapshot = ItemID.Sets.Factory.CreateIntSet(-1);
			thread.itemTransmuteAvailableSnapshot = ItemID.Sets.Factory.CreateBoolSet(false);

			for (int i = 0; i < ItemLoader.ItemCount; i++) {
				var info = MagicCache.ShimmerInfos[i];
				var attempt = info.GetAttempt(out int decraftingRecipeIndex);

				if (attempt.IsSuccessfulButNotDecraftable())
					thread.itemTransmuteAvailableSnapshot[i] = true;
				else if (attempt.IsSuccessful())
					thread.itemTypeToDecraftRecipeIndexSnapshot[i] = decraftingRecipeIndex;
			}
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

		private static void RefreshStorageItems(CraftingGUI.CommonCraftingThread thread) {
			NetHelper.Report(true, "Updating stored ingredients collection and result item...");

			int selectedItem = -1;
			IEnumerable<ItemReport> cachedShimmerReports = null;

			if (thread is ShimmeringRefreshThread shimmeringThread) {
				selectedItem = shimmeringThread.selectedItem;
				cachedShimmerReports = shimmeringThread.cachedShimmerReports;
			} else if (thread is ShimmerInfoPanelRefreshThread ingredientsThread) {
				selectedItem = ingredientsThread.selectedItem;
				cachedShimmerReports = ingredientsThread.cachedShimmerReports;
			}

			if (selectedItem <= ItemID.None) {
				thread.InitAsCompleted("Populating Stored Ingredients");
				NetHelper.Report(true, "Failed.  No item is selected.");
				return;
			}

			thread.InitTaskSchedule(thread.resultItemGroups.Count, "Populating Stored Ingredients");

			var handler = thread.recipeItemsHandler = new ZoneResultItemsHandler(thread);

			foreach (var items in thread.resultItemGroups.NotifyStepsTo(thread)) {
				foreach (Item item in items) {
					CraftingGUI.CheckItemFromSource(handler, item, selectedItem, IsItemValidForStorage);

					if (IsItemValidForResult(item, selectedItem, cachedShimmerReports))
						handler.SetResultItem(item);
				}
			}

			handler.CompactCollections();

			NetHelper.Report(true, $"Success! Found {handler.StoredIngredientCount} items and {(handler.FoundStoredResultItem ? "no" : $"{((ZoneResultItemsHandler)handler).resultItems.Count}")} result items");
		}

		internal static bool IsItemValidForStorage(Item item, int selectedItem) => item.type == selectedItem && item.stack > 0;

		internal static bool IsItemValidForResult(Item item) {
			if (MagicUI.HasActiveThread(out ShimmeringRefreshThread thread))
				return IsItemValidForResult(item, thread.selectedItem, thread.cachedShimmerReports);
			else if (MagicUI.HasActiveThread(out ShimmerInfoPanelRefreshThread ingredientsThread))
				return IsItemValidForResult(item, ingredientsThread.selectedItem, ingredientsThread.cachedShimmerReports);

			if (selectedItem == -1)
				return false;

			IShimmerResultReport report = new ItemReport(item.type);

			// Need to check the reports manually
			foreach (var cachedReport in MagicCache.ShimmerInfos[selectedItem].GetShimmerReports().OfType<ItemReport>()) {
				if (cachedReport.Equals(report))
					return true;
			}

			return false;
		}

		private static bool IsItemValidForResult(Item item, int selectedItem, IEnumerable<ItemReport> cachedShimmerReports) {
			if (selectedItem == -1)
				return false;

			IShimmerResultReport report = new ItemReport(item.type);

			if (selectedItem == -1)
				return false;

			foreach (var cachedReport in cachedShimmerReports) {
				if (cachedReport.Equals(report))
					return true;
			}

			return false;
		}
	}
}
