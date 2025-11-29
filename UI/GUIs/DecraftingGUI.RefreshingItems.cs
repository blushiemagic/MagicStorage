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

		private static void RefreshItemsAvailability(ShimmeringRefreshThread thread) {
			if (thread.itemsToRefresh is null)
				RefresAllItemsAvailability(thread);  //Refresh all items
			else
				RefreshSpecificItemsAvailablity(thread);

			NetHelper.Report(false, "Visible items: " + thread.viewingItems.Count);
			NetHelper.Report(false, "Available items: " + thread.viewingItemIsAvailable.Count(static b => b));
		}

		private static void RefresAllItemsAvailability(ShimmeringRefreshThread thread) {
			NetHelper.Report(true, "Refreshing all items");

			// Each DoFiltering does: GetItems, SortItems, adding items, adding item availability
			// Each GetItems does: loading base items, applying text/mod filters
			// Each SortItems does: DoSorting, blacklist filtering, favorite checks

			thread.InitTaskSchedule(9, "Refreshing items");

			PopulateViewingItems(thread, attempt: 0);

			bool didDefault = false;
			ref string errorText = ref thread.searchBarError;

			// now if nothing found we disable filters one by one
			if (thread.controls.fullSearchText.Trim().Length > 0)
			{
				if (thread.viewingItems.Count == 0 && (thread.globalHiddenTypes.Count > 0 || thread.hiddenTypes.Count > 0))
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with no hidden recipes");

					// search hidden recipes too
					thread.globalHiddenTypes.Clear();
					thread.hiddenTypes.Clear();

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.DecraftingNoBlacklist");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

					PopulateViewingItems(thread, attempt: 1);
				}

				if (thread.viewingItems.Count == 0 && thread.controls.modSearchOption != ModSearchBox.ModIndexAll)
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with All Mods setting");

					// search all mods
					thread.controls = thread.controls.CreateCopy(
						modSearchOptionOverride: ModSearchBox.ModIndexAll
					);

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.DecraftingDefaultToAllMods");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

					PopulateViewingItems(thread, attempt: 2);
				}
			}

			for (int i = 0; i < thread.viewingItems.Count; i++) {
				int item = thread.viewingItems[i];
				bool available = thread.viewingItemIsAvailable[i];

				MagicUI.AddRefreshWatchdog(new ItemWatchTarget(item), available);
			}

			if (!didDefault)
				errorText = null;
		}

		internal static void PopulateViewingItems(ShimmeringRefreshThread thread, int attempt) {
			CraftingGUI.PopulateCollections(
				thread: thread,
				sortedAndFilteredObjects: ItemSorter.SortAndFilterShimmerableItems(thread, attempt, provider: thread.shimmerableItemFilterProvider),
				destination: thread.viewingItems,
				destinationAvailable: thread.viewingItemIsAvailable,
				isObjectAvailable: IsAvailable,
				objectNameForTask: "Shimmerable Items"
			);
		}

		internal static bool forceSpecificItemResort;

		private static void RefreshSpecificItemsAvailablity(ShimmeringRefreshThread thread) {
			CraftingGUI.RefreshSpecificObjects(
				thread: thread,
				provider: thread.shimmerableItemFilterProvider,
				refreshingObjects: thread.itemsToRefresh,
				destination: thread.viewingItems,
				destinationAvailable: thread.viewingItemIsAvailable,
				getItem: Utility.GetItemSample,
				getItemType: type => type,
				canProcessObject: type => type > ItemID.None,
				isObjectAvailable: IsAvailable,
				objectNameForTask: "Shimmerable Items",
				forcedResort: ref forceSpecificItemResort
			);
		}
	}
}
