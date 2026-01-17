using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Sorting;
using System;
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

		private static void RefreshItemsAvailability<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<int>, IMainZoneObjectResultsProvider<int>, IShimmerSnapshotsProvider
		{
			if (thread.MainZoneObjectsResults.objectsToRefresh is not { Count: > 0 })
				RefreshAllItemsAvailability(thread);  //Refresh all items
			else
				RefreshSpecificItemsAvailablity(thread);

			NetHelper.Report(false, "Visible items: " + thread.MainZoneObjectsResults.objects.Count);
			NetHelper.Report(false, "Available items: " + thread.MainZoneObjectsResults.objectIsAvailable.Count(static b => b));
		}

		private static void RefreshAllItemsAvailability<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<int>, IMainZoneObjectResultsProvider<int>, IShimmerSnapshotsProvider
		{
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
				var controls = thread.MainZoneObjectsFilterControls;
				var results = thread.MainZoneObjectsResults.objects;

				if (results.Count == 0 && (controls.globalHiddenTypes.Count > 0 || controls.hiddenTypes.Count > 0))
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with no hidden recipes");

					// search hidden recipes too
					controls.globalHiddenTypes.Clear();
					controls.hiddenTypes.Clear();

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.DecraftingNoBlacklist");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

					PopulateViewingItems(thread, attempt: 1);
				}

				if (results.Count == 0 && thread.controls.modSearchOption != ModSearchBox.ModIndexAll)
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

			foreach (var (item, available) in thread.MainZoneObjectsResults.Enumerate())
				MagicUI.AddRefreshWatchdog(new ItemWatchTarget(item), available);

			if (!didDefault)
				errorText = null;
		}

		internal static void PopulateViewingItems<T>(T thread, int attempt)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<int>, IMainZoneObjectResultsProvider<int>, IShimmerSnapshotsProvider
		{
			CraftingGUI.PopulateCollections(
				thread,
				ItemSorter.SortAndFilterShimmerableItems(thread, attempt),
				IsAvailable,
				"Shimmerable Items"
			);
		}

		internal static bool forceSpecificItemResort;

		private static void RefreshSpecificItemsAvailablity<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<int>, IMainZoneObjectResultsProvider<int>, IShimmerSnapshotsProvider
		{
			CraftingGUI.RefreshSpecificObjects<T, int>(
				thread,
				Utility.GetItemSample,
				x => x,
				type => type > ItemID.None,
				IsAvailable,
				x => x,
				_ => null,
				"Shimmerable Items",
				ref forceSpecificItemResort
			);
		}
	}
}
