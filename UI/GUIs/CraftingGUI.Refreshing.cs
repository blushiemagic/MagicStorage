using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.UI;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static readonly List<Item> items = new();
		internal static readonly List<List<Item>> itemGroups = new();

		internal static readonly Dictionary<int, int> itemCounts = new();
		internal static readonly Dictionary<int, Dictionary<int, int>> itemCountsByPrefix = new();

		internal static readonly HashSet<int> isItemInfinite = [];
		internal static bool allItemsAreInfinite;

		// Only used by DoWithdrawResult to check items from modules
		internal static readonly List<Item> sourceItemsFromModules = new();

		// Caches for StoredIngredientsRefreshThread
		internal static readonly ConditionalWeakTable<Item, object> wasModuleItem = [];
		internal static readonly ConditionalWeakTable<Item, object> moduleItemWasFromInventory = [];
		
		[Obsolete("Use MagicUI.RefreshItems() instead", error: true)]
		public static void RefreshItems() => MagicUI.RefreshItems();

		internal static void ResetRefreshCache() {
			recipesToRefresh = null;
		}
		
		internal static void RefreshItems_Inner() {
			if (MagicUI.ForceNextRefreshToBeFull) {
				// Force all recipes to be recalculated
				recipesToRefresh = null;
			}

			// Always reset the cached values
			ResetRecentRecipeCache();

			lastKnownRecursionErrorForStoredItems = null;

			NetHelper.Report(true, "CraftingGUI: RefreshItems invoked");

			GetCommonRefreshThreadParameters(out var adjTiles, out var showAllIngredients, out var blockedStoredIngredients, out var craftAmountTarget);

			var thread = new CraftingRefreshThread(
				controls: CreateRefreshThreadControls(),
				adjTiles: adjTiles,
				selectedRecipe: selectedRecipe,
				recipesToRefresh: recipesToRefresh,
				showAllIngredients: showAllIngredients,
				recipeFilter: MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>().recipeButtons.Choice,
				favorited: StoragePlayer.LocalPlayer.FavoritedRecipes,
				hidden: StoragePlayer.LocalPlayer.HiddenRecipes,
				configBlacklist: MagicStorageConfig.GlobalRecipeBlacklist,
				blockedStoredIngredients: blockedStoredIngredients,
				craftAmountTarget: craftAmountTarget
			);
			thread.SetDebugName("CraftingGUI thread");
			thread.Start();

			ResetRefreshCache();
		}

		private static StorageViewControls CreateRefreshThreadControls() {
			var craftingPage = MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>();

			return new StorageViewControls(
				sortingOption: SortingOptionLoader.Selected,
				filteringOption: FilteringOptionLoader.Selected,
				generalFilters: FilteringOptionLoader.GeneralSelections,
				fullSearchText: craftingPage.searchBar.State.InputText,
				showOnlyFavorites: MagicStorageConfig.CraftingFavoritingEnabled && craftingPage.recipeButtons.Choice == RecipeButtonsFavoritesChoice,
				modSearchOption: craftingPage.modSearchBox.ModIndex
			);
		}

		internal static void GetCommonRefreshThreadParameters(
			out IEnumerable<bool> adjTiles,
			out bool showAllIngredients,
			out IEnumerable<ItemData> blockedStoredIngredients,
			out int craftAmountTarget
		) {
			adjTiles = CraftingGUI.adjTiles;
			showAllIngredients = ((CraftingUIState)MagicUI.craftingUI).recursionButton.IsOn;
			blockedStoredIngredients = blockStorageItems;
			craftAmountTarget = CraftingGUI.craftAmountTarget;
		}

		internal static void GetCommonRefreshThreadParameters(
			out IEnumerable<bool> adjTiles,
			out IEnumerable<ItemData> blockedStoredIngredients,
			out int craftAmountTarget
		) {
			adjTiles = CraftingGUI.adjTiles;
			blockedStoredIngredients = blockStorageItems;
			craftAmountTarget = CraftingGUI.craftAmountTarget;
		}

		private static bool AvailableForSnapshot(Recipe r) => !r.Disabled && RecipeLoader.RecipeAvailable(r);

		internal static bool CheckForCreativeUnit(EnvironmentSandbox sandbox) => sandbox.heart is { } heart && heart.GetStorageUnits().OfType<TECreativeStorageUnit>().Any();

		internal static HashSet<int> LoadInfiniteItems(EnvironmentSandbox sandbox) {
			var infiniteItems = InfiniteItemsForCrafting.GetInfiniteItems();
			
			if (sandbox.heart is not null) {
				foreach (var module in sandbox.heart.GetModules()) {
					var items = module.GetInfiniteItems(sandbox);

					if (items is not null && items.Any())
						infiniteItems.UnionWith(items);
				}
			}

			return infiniteItems;
		}

		private static void SortAndFilter(CraftingRefreshThread thread) {
			LoadItemsAndSetDictionaryInfo(thread);
			RefreshStorageItems(thread);
			RefreshRecipes(thread);
		}

		// Moved to internal method for use by DecraftingGUI
		internal static void LoadItemsAndSetDictionaryInfo(CraftingControlsRefreshThread thread) {
			// Organize the items from the storage system
			thread.workingItemList = thread.allStoredItems;
			thread.workingCounter = thread.allStoredItems.Count;
			thread.workingFlag = false;

			var storedItems = ItemSorter.SortAndFilterItems(thread, 0);

			thread.resultItems.Clear();
			thread.resultItemGroups.Clear();

			thread.resultItems.AddRange(storedItems);

			thread.aggregateResults.CopyResultGroupsTo(thread.resultItemGroups);

			int numModuleItems = 0;
			thread.resultItemsFromModules.Clear();

			if (thread.allModuleItems is { Count: > 0 }) {
				// Organize the items from the modules
				thread.workingItemList = thread.allModuleItems;
				thread.workingCounter = thread.allModuleItems.Count;
				thread.workingFlag = true;  // uniqueSlotPerItemStack

				var moduleItems = ItemSorter.SortAndFilterItems(thread, 0, listClassification: "Module");

				thread.resultItems.AddRange(moduleItems);

				thread.resultItemsFromModules.AddRange(thread.aggregateResults.GetAllSourceItems());

				numModuleItems = moduleItems.Count;
			}

			SetCountsDictionaries(thread);

			thread.workingItemList = null;
			thread.workingCounter = 0;
			thread.workingFlag = false;

			NetHelper.Report(false, "Total items: " + thread.resultItems.Count);
			NetHelper.Report(false, "Items from modules: " + numModuleItems);
		}

		internal static void SetCountsDictionaries(CommonCraftingThread thread) {
			var itemCounts = thread.itemCounts;
			var itemCountsByPrefix = thread.itemCountsByPrefix;

			itemCounts.Clear();
			itemCountsByPrefix.Clear();

			thread.InitTaskSchedule(thread.resultItems.Count, "Counting Items");

			// Previously just used GroupBy, but that doesn't play nice for multiple element data
			foreach (Item item in thread.resultItems.NotifyStepsTo(thread)) {
				if (itemCounts.TryGetValue(item.type, out int quantity))
					itemCounts[item.type] = new ClampedArithmetic(quantity) + item.stack;
				else
					itemCounts[item.type] = item.stack;

				if (itemCountsByPrefix.TryGetValue(item.type, out var prefixCounts)) {
					if (prefixCounts.TryGetValue(item.prefix, out quantity))
						prefixCounts[item.prefix] = new ClampedArithmetic(quantity) + item.stack;
					else
						prefixCounts[item.prefix] = item.stack;
				} else
					itemCountsByPrefix[item.type] = new Dictionary<int, int>() { [item.prefix] = item.stack };
			}
		}
	}
}
