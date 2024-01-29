using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace MagicStorage {
	partial class DecraftingGUI {
		private class ItemWatchTarget : IRefreshUIWatchTarget {
			private readonly int _itemType;

			public ItemWatchTarget(int itemType) {
				_itemType = itemType;
			}

			public bool GetCurrentState() => IsAvailable(_itemType);

			public void OnStateChange(out bool forceFullRefresh) {
				SetNextDefaultItemCollectionToRefresh(_itemType);
				forceFullRefresh = false;
			}
		}

		private static void SafelyRefreshItems(StorageGUI.ThreadContext thread, ThreadState state) {
			try {
				if (state.itemsToRefresh is null)
					RefreshItemsAvailability(thread, state);  //Refresh all items
				else
					RefreshSpecificItemsAvailablity(thread, state);

				NetHelper.Report(false, "Visible items: " + viewingItems.Count);
				NetHelper.Report(false, "Available items: " + itemAvailable.Count(static b => b));

				NetHelper.Report(true, "Item refreshing finished");
			} catch (Exception e) {
				Main.QueueMainThreadAction(() => Main.NewTextMultiline(e.ToString(), c: Color.White));
			}
		}

		private static void RefreshItemsAvailability(StorageGUI.ThreadContext thread, ThreadState state) {
			NetHelper.Report(true, "Refreshing all items");

			// Each DoFiltering does: GetItems, SortItems, adding items, adding item availability
			// Each GetItems does: loading base items, applying text/mod filters
			// Each SortItems does: DoSorting, blacklist filtering, favorite checks

			thread.InitTaskSchedule(9, "Refreshing items");

			var query = new CraftingGUI.Query<int>(new CraftingGUI.QueryResults<int>(viewingItems, itemAvailable),
				ItemSorter.GetShimmerItems,
				SortItems,
				IsAvailable);

			CraftingGUI.DoFiltering(thread, state, query);

			bool didDefault = false;

			// now if nothing found we disable filters one by one
			if (thread.searchText.Length > 0)
			{
				if (viewingItems.Count == 0 && (state.globalHiddenTypes.Count > 0 || state.hiddenTypes.Count > 0))
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with no hidden recipes");

					// search hidden recipes too
					state.globalHiddenTypes = CraftingGUI.CommonCraftingState.EmptyGlobalHiddenTypes;
					state.hiddenTypes = ItemTypeOrderedSet.Empty;

					MagicUI.lastKnownSearchBarErrorReason = Language.GetTextValue("Mods.MagicStorage.Warnings.DecraftingNoBlacklist");
					didDefault = true;

					thread.ResetTaskCompletion();

					CraftingGUI.DoFiltering(thread, state, query);
				}

				if (viewingItems.Count == 0 && thread.modSearch != ModSearchBox.ModIndexAll)
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with All Mods setting");

					// search all mods
					thread.modSearch = ModSearchBox.ModIndexAll;

					MagicUI.lastKnownSearchBarErrorReason = Language.GetTextValue("Mods.MagicStorage.Warnings.DecraftingDefaultToAllMods");
					didDefault = true;

					thread.ResetTaskCompletion();

					CraftingGUI.DoFiltering(thread, state, query);
				}
			}

			for (int i = 0; i < viewingItems.Count; i++) {
				int item = viewingItems[i];
				bool available = itemAvailable[i];

				MagicUI.AddRefreshWatchdog(new ItemWatchTarget(item), available);
			}

			if (!didDefault)
				MagicUI.lastKnownSearchBarErrorReason = null;
		}

		internal static bool forceSpecificItemResort;

		private static void RefreshSpecificItemsAvailablity(StorageGUI.ThreadContext thread, ThreadState state) {
			var query = new CraftingGUI.SpecificQuery<int>(new CraftingGUI.QueryResults<int>(viewingItems, itemAvailable),
				SortItems,
				IsAvailable,
				IsItemValidForQuery,
				CanBeAdded);

			CraftingGUI.RefreshSpecificQueryItems(thread, state, state.itemsToRefresh, query, forceSpecificItemResort, "items");

			forceSpecificItemResort = false;
		}

		private static bool IsItemValidForQuery(StorageGUI.ThreadContext thread, int item) => item > ItemID.None && ItemSorter.ItemPassesFilter(item, thread);

		private static bool CanBeAdded(StorageGUI.ThreadContext thread, CraftingGUI.CommonCraftingState state, int item) {
			var sample = ContentSamples.ItemsByType[item];
			return FilteringOptionLoader.Get(thread.filterMode).Filter(sample) && CraftingGUI.DoesItemPassFilters(thread, state, sample);
		}

		private static IEnumerable<int> SortItems(StorageGUI.ThreadContext thread, CraftingGUI.CommonCraftingState state, IEnumerable<int> source) {
			IEnumerable<int> sortedItems = ItemSorter.DoSorting(thread, source, Utility.GetItemSample);

			thread.CompleteOneTask();

			// show only blacklisted recipes only if choice = 2, otherwise show all other
			if (MagicStorageConfig.RecipeBlacklistEnabled)
				sortedItems = sortedItems.Where(x => state.recipeFilterChoice == CraftingGUI.RecipeButtonsBlacklistChoice == state.IsHidden(x));

			thread.CompleteOneTask();

			// favorites first
			if (MagicStorageConfig.CraftingFavoritingEnabled) {
				sortedItems = sortedItems.Where(x => state.recipeFilterChoice != CraftingGUI.RecipeButtonsFavoritesChoice || state.favoritedTypes.Contains(ContentSamples.ItemsByType[x]));
					
				sortedItems = sortedItems.OrderByDescending(x => state.favoritedTypes.Contains(ContentSamples.ItemsByType[x]) ? 1 : 0);
			}

			thread.CompleteOneTask();

			return sortedItems;
		}
	}
}
