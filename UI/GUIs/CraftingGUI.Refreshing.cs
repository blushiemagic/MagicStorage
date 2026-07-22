using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static readonly List<Item> items = new();
		internal static readonly List<List<Item>> itemGroups = new();

		internal static readonly Dictionary<int, int> itemCounts = new();
		internal static readonly Dictionary<int, Dictionary<int, int>> itemCountsByPrefix = new();
		internal static readonly StaticValue<int> itemCountsHash = new();

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

		internal static void ResetRefreshCache() => ClearRecipeRefreshOptimizationState();
		
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

		private static void SortAndFilter(CraftingRefreshThread thread) {
			LoadItemsAndSetDictionaryInfo(thread);
			PrepareInventoryCraftabilityGraph(thread, buildIfCacheMiss: true);
			RefreshStorageItems(thread);
			RefreshRecipes(thread);
		}

		// Moved to internal method for use by DecraftingGUI
		internal static void LoadItemsAndSetDictionaryInfo<T>(T thread)
			where T : RefreshThread, IStorageItemsPovider, IProcessedStorageItemsProvider
		{
			LoadItemsAndSetDictionaryInfo(thread, thread.StorageItems);
		}

		internal static void LoadItemsAndSetDictionaryInfo<T>(T thread, StorageItems storage)
			where T : RefreshThread, IProcessedStorageItemsProvider
		{
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

			SetCountsDictionaries(thread, storage.allStoredItems.Concat(processed.allModuleItems ?? []));

			thread.workingItemList = null;
			thread.workingCounter = 0;
			thread.workingFlag = false;

			NetHelper.Report(false, "Total items: " + processed.resultItems.Count);
			NetHelper.Report(false, "Items from modules: " + numModuleItems);
		}

		internal static void LoadInventoryCountsOnly<T>(T thread, StorageItems storage)
			where T : RefreshThread, IProcessedStorageItemsProvider
		{
			SetCountsDictionaries(thread, storage.allStoredItems.Concat(thread.ProcessedStorageItems.allModuleItems ?? []));
		}

		internal static void SetCountsDictionaries<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider
		{
			SetCountsDictionaries(thread, thread.ProcessedStorageItems.resultItems.Value);
		}

		internal static void SetCountsDictionaries<T>(T thread, IEnumerable<Item> sourceItems)
			where T : RefreshThread, IProcessedStorageItemsProvider
		{
			var processed = thread.ProcessedStorageItems;

			var itemCounts = processed.itemCounts;
			var itemCountsByPrefix = processed.itemCountsByPrefix;

			itemCounts.Clear();
			itemCountsByPrefix.Clear();
			processed.itemCountsHash.Value = 0;

			int totalItems = sourceItems.TryGetNonEnumeratedCount(out int count) ? count : 0;
			thread.InitTaskSchedule(totalItems, "Counting Items");

			foreach (Item item in sourceItems.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				if (item is not { IsAir: false })
					continue;

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

			processed.itemCountsHash.Value = GetCountsHash(itemCounts.Value);
		}
	}
}
