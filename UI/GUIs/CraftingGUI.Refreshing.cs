using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;

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

		internal static bool hasCompleteData;
		
		[Obsolete("Use MagicUI.RefreshItems() instead", error: true)]
		public static void RefreshItems() => MagicUI.RefreshItems();

		internal static void ResetRefreshCache() {
			recipesToRefreshByIndex = null;
		}
		
		internal static void RefreshItems_Inner() {
			// Always reset the cached values
			ResetRecentRecipeCache();

			lastKnownRecursionErrorForStoredItems = null;

			NetHelper.Report(true, "CraftingGUI: RefreshItems invoked");

			if (recipesToRefreshByIndex is { Count: > 0 })
				NetHelper.Report(false, $"Refreshing {recipesToRefreshByIndex.Count} recipes...");

			CreateFullRefreshThread(caller: "CraftingGUI.RefreshItems()").Start();

			ResetRefreshCache();
		}

		private class FullRefreshBuilder : IRefreshThreadBuilder {
			public static IRefreshThreadBuilder Instance { get; } = new FullRefreshBuilder();

			public StorageViewControls CreateControls() => CreateRefreshThreadControls(MagicUI.craftingUI);

			public RefreshThread CreateThread(StorageViewControls controls) {
				// Force all recipes to be recalculated
				if (MagicUI.ForceNextRefreshToBeFull)
					recipesToRefreshByIndex = null;

				return new CraftingRefreshThread(
					controls: controls,
					processedStorage: new(
						staticWasModuleItemTable: wasModuleItem,
						staticModuleItemWasFromInventoryTable: moduleItemWasFromInventory,
						staticResultItemsList: items,
						staticResultItemGroupsList: itemGroups,
						staticResultItemsFromModulesList: sourceItemsFromModules,
						staticCountsDictionary: itemCounts,
						staticCountsByPrefixDictionary: itemCountsByPrefix
					),
					mainZoneControls: new(
						zoneObjectFilterChoice: MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>().recipeButtons.Choice,
						favorited: StoragePlayer.LocalPlayer.FavoritedRecipes,
						hidden: StoragePlayer.LocalPlayer.HiddenRecipes,
						configBlacklist: MagicStorageConfig.GlobalRecipeBlacklist
					),
					mainZoneResults: new(
						objectsToRefresh: CollectRefreshingRecipes(),
						staticObjectList: recipes,
						staticAvailableList: recipeAvailable
					),
					ingredientControls: new(
						staticShowAllIngredientsField: new ShowAllIngredientsProvider(((CraftingUIState)MagicUI.craftingUI).recursionButton.IsOn),
						staticInfiniteItemsSet: isItemInfinite,
						staticBlockedList: blockStorageItems,
						staticCreativeUnitField: new CreativeUnitPresentProvider()
					),
					craftingObject: new(
						selection: new SelectionProvider(),
						craftAmountTarget: new CraftAmountTargetProvider()
					),
					availableCache: new(
						staticTable: recipeToAvailableLookup
					)
				);
			}
		}

		private class ShowAllIngredientsProvider(bool defaultValue) : IReadOnlyValueProvider<bool> {
			public bool Value { get; private set; } = defaultValue;

			public void ClearStatic() => showAllPossibleIngredients = false;
			public void CopyFromStatic() => Value = showAllPossibleIngredients;
			public void CopyToStatic() => showAllPossibleIngredients = Value;
		}

		private class SelectionProvider : IReadOnlyValueProvider<Recipe> {
			public Recipe Value { get; private set; }
			public SelectionProvider() => CopyFromStatic();
			public SelectionProvider(Recipe defaultValue) => Value = defaultValue;
			public void ClearStatic() { }
			public void CopyFromStatic() => Value = selectedRecipe;
			public void CopyToStatic() => selectedRecipe = Value;
		}

		internal class CraftAmountTargetProvider : IValueProvider<int> {
			public int Value { get; set; }
			public CraftAmountTargetProvider() => CopyFromStatic();
			public CraftAmountTargetProvider(int defaultValue) => Value = defaultValue;
			public void ClearStatic() => craftAmountTarget = 1;
			public void CopyFromStatic() => Value = craftAmountTarget;
			public void CopyToStatic() => craftAmountTarget = Value;
		}

		internal class CreativeUnitPresentProvider : IValueProvider<bool> {
			public bool Value { get; set; }
			public void ClearStatic() => allItemsAreInfinite = false;
			public void CopyFromStatic() => Value = allItemsAreInfinite;
			public void CopyToStatic() => allItemsAreInfinite = Value;
		}

		internal static StorageViewControls CreateRefreshThreadControls(BaseStorageUI refreshingUI) {
			var craftingPage = refreshingUI.GetDefaultPage<CraftingUIState.RecipesPage>();

			return new StorageViewControls(
				sortingOption: SortingOptionLoader.Selected,
				filteringOption: FilteringOptionLoader.Selected,
				generalFilters: FilteringOptionLoader.GeneralSelections,
				fullSearchText: craftingPage.searchBar.State.InputText,
				showOnlyFavorites: MagicStorageConfig.CraftingFavoritingEnabled && craftingPage.recipeButtons.Choice == RecipeButtonsFavoritesChoice,
				modSearchOption: craftingPage.modSearchBox.ModIndex
			);
		}

		private static void SortAndFilter(CraftingRefreshThread thread) {
			LoadItemsAndSetDictionaryInfo(thread);
			RefreshStorageItems(thread);
			RefreshRecipes(thread);
		}

		// Moved to internal method for use by DecraftingGUI
		internal static void LoadItemsAndSetDictionaryInfo<T>(T thread)
			where T : RefreshThread, IStorageItemsPovider, IProcessedStorageItemsProvider
		{
			var storage = thread.StorageItems;
			var processed = thread.ProcessedStorageItems;

			// Organize the items from the storage system
			thread.workingItemList = storage.allStoredItems;
			thread.workingCounter = storage.allStoredItems.Count;
			thread.workingFlag = false;

			var storedItems = ItemSorter.SortAndFilterItems(thread, 0);

			processed.resultItems.Clear();
			processed.resultItemGroups.Clear();

			processed.resultItems.AddRange(storedItems);

			thread.aggregateResults.CopyResultGroupsTo(processed.resultItemGroups.Value);

			int numModuleItems = 0;
			processed.resultItemsFromModules.Clear();

			if (processed.allModuleItems is { Count: > 0 }) {
				// Organize the items from the modules
				thread.workingItemList = processed.allModuleItems;
				thread.workingCounter = processed.allModuleItems.Count;
				thread.workingFlag = true;  // uniqueSlotPerItemStack

				var moduleItems = ItemSorter.SortAndFilterItems(thread, 0, listClassification: "Module");

				processed.resultItems.AddRange(moduleItems);

				processed.resultItemsFromModules.AddRange(thread.aggregateResults.GetAllSourceItems());

				numModuleItems = moduleItems.Count;
			}

			SetCountsDictionaries(thread);

			thread.workingItemList = null;
			thread.workingCounter = 0;
			thread.workingFlag = false;

			NetHelper.Report(false, "Total items: " + processed.resultItems.Count);
			NetHelper.Report(false, "Items from modules: " + numModuleItems);
		}

		internal static void SetCountsDictionaries<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider
		{
			var processed = thread.ProcessedStorageItems;

			var itemCounts = processed.itemCounts;
			var itemCountsByPrefix = processed.itemCountsByPrefix;

			itemCounts.Clear();
			itemCountsByPrefix.Clear();

			thread.InitTaskSchedule(processed.resultItems.Count, "Counting Items");

			foreach (Item item in processed.resultItems.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
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
