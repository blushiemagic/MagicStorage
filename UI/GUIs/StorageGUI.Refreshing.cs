using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using MagicStorage.UI.States;
using System.Collections.Generic;
using System.Linq;
using Terraria.Localization;
using Terraria;
using System;
using MagicStorage.Common.Threading.Refreshing;
using System.Runtime.CompilerServices;

namespace MagicStorage {
	partial class StorageGUI {
		#region Obsolete stuff

		// Field included for backwards compatibility, but made Obsolete to encourage modders to use the new API
		[Obsolete("Use the SetRefresh() method or RefreshUI property in MagicUI instead", error: true)]
		public static bool needRefresh;

		[Obsolete]
		internal static ref bool Obsolete_needRefresh() => ref needRefresh;

		[Obsolete("Use MagicUI.RefreshUI instead", error: true)]
		public static bool RefreshUI {
			get => MagicUI.RefreshUI;
			set => MagicUI.RefreshUI = value;
		}

		[Obsolete("Use MagicUI.CurrentlyRefreshing instead", error: true)]
		public static bool CurrentlyRefreshing { get; internal set; }

		[Obsolete("Use MagicUI.OnRefresh instead", error: true)]
		public static event Action OnRefresh;
		
		[Obsolete("Use MagicUI.ForceNextRefreshToBeFull instead", error: true)]
		public static bool ForceNextRefreshToBeFull {
			get => MagicUI.ForceNextRefreshToBeFull;
			set => MagicUI.ForceNextRefreshToBeFull = value;
		}

		/// <inheritdoc cref="MagicUI.SetRefresh"/>
		[Obsolete("Use MagicUI.SetRefresh() instead", error: true)]
		public static void SetRefresh(bool forceFullRefresh = false) => MagicUI.SetRefresh(forceFullRefresh);

		internal static readonly List<Item> items = new();
		internal static readonly ConditionalWeakTable<Item, List<Item>> itemToSourceItems = new();
		// NOTE: Removed because ItemID.Sets.IsAMaterial[] will always be read in Item.SetDefaults() for items in storage
	//	internal static readonly List<bool> didMatCheck = new();

		[Obsolete("Use MagicUI.RefreshItems() instead", error: true)]
		public static void RefreshItems() {
			// Moved to the start of the logic since CheckRefresh() might be called multiple times during refreshing otherwise
			MagicUI.RefreshUI = false;
			Obsolete_needRefresh() = false;

			// No refreshing required
			if (StoragePlayer.IsStorageEnvironment()) {
				ResetRefreshCache();
				return;
			}

			if (StoragePlayer.IsStorageCrafting()) {
				CraftingGUI.RefreshItems();
				ResetRefreshCache();
				return;
			}

			if (StoragePlayer.IsStorageDecrafting()) {
				DecraftingGUI.RefreshItems();
				ResetRefreshCache();
				return;
			}

			CraftingGUI.ResetRefreshCache();

			RefreshItems_Inner();
		}

		#endregion

		internal static void ResetRefreshCache() {
			itemTypesToUpdate = null;
		}

		internal static void RefreshItems_Inner() {
			// Prevent inconsistencies after refreshing items
			actionSlotFocus = -1;

			CreateFullRefreshThread(caller: "StorageGUI.RefreshItems()").Start();

			ResetRefreshCache();
		}

		public static RefreshThread CreateFullRefreshThread(string caller) {
			// Force full refresh if item deletion mode is active
			if (MagicUI.ForceNextRefreshToBeFull || currentMode is ActionMode.Deletion)
				itemTypesToUpdate = null;

			var storagePage = MagicUI.storageUI.GetDefaultPage<StorageUIState.StoragePage>();

			var controls = new StorageViewControls(
				sortingOption: SortingOptionLoader.Selected,
				filteringOption: FilteringOptionLoader.Selected,
				generalFilters: FilteringOptionLoader.GeneralSelections,
				fullSearchText: storagePage.searchBar.State.InputText,
				showOnlyFavorites: MagicStorageConfig.CraftingFavoritingEnabled && storagePage.filterFavorites.Value,
				modSearchOption: storagePage.modSearchBox.ModIndex
			);

			var thread = new StorageRefreshThread(controls, currentMode, itemTypesToUpdate);
			thread.SetDebugName($"{caller} thread");
			return thread;
		}

		private static IEnumerable<Item> AdjustToUpdateSet(IEnumerable<Item> source, HashSet<int> targetItemTypes) {
			List<Item> itemsToUpdate = [];

			foreach (Item item in source) {
				if (!targetItemTypes.Contains(item.type))
					yield return item;
				else
					itemsToUpdate.Add(item);
			}

			foreach (Item item in itemsToUpdate)
				yield return item;
		}

		private static IEnumerable<Item> AdjustToDepositHistory(StorageRefreshThread thread, IEnumerable<Item> source) {
			// Organize the source items by their type according to the most recent deposit history
			Dictionary<int, List<Item>> stored = source.GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.ToList());
			List<Item> depositHistory = [.. thread.Heart.UniqueItemsPutHistory];

			for (int i = depositHistory.Count - 1; i >= 0; i--) {
				Item item = depositHistory[i];

				if (stored.TryGetValue(item.type, out var sourceItems)) {
					foreach (Item sourceItem in sourceItems)
						yield return sourceItem;
				}
			}
		}

		private static void SortAndFilter(RefreshThread thread) {
			PopulateItems(thread, attempt: 0);
			
			bool didDefault = false;
			ref string errorText = ref thread.searchBarError;

			// now if nothing found we disable filters one by one
			if (thread.controls.fullSearchText.Trim().Length > 0)
			{
				if (items.Count == 0 && thread.controls.filteringOption != FilteringOptionLoader.Definitions.All.Type)
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with All setting");

					// search all categories
					thread.controls = thread.controls.CreateCopy(
						filteringOptionOverride: FilteringOptionLoader.Definitions.All.Type
					);

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.StorageDefaultToAllItems");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

					PopulateItems(thread, attempt: 1);
				}

				if (items.Count == 0 && thread.controls.modSearchOption != ModSearchBox.ModIndexAll)
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with All Mods setting");

					// search all mods
					thread.controls = thread.controls.CreateCopy(
						modSearchOptionOverride: ModSearchBox.ModIndexAll
					);

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.StorageDefaultToAllMods");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

					PopulateItems(thread, attempt: 2);
				}
			}

			if (!didDefault)
				errorText = null;
		}

		internal const int RECENT_FILTER_ITEM_COUNT = 100;

		private static void PopulateItems(RefreshThread thread, int attempt) {
			List<Item> resultItems;

			if (thread.controls.filteringOption == FilteringOptionLoader.Definitions.Recent.Type) {
				if (thread.controls.sortingOption == SortingOptionLoader.Definitions.Default.Type) {
					// Force the sorting option to be ignored
					thread.controls = thread.controls.CreateCopy(
						filteringOptionOverride: FilteringOptionLoader.Definitions.All.Type,
						sortingOptionOverride: -1
					);
				} else {
					thread.controls = thread.controls.CreateCopy(
						filteringOptionOverride: FilteringOptionLoader.Definitions.All.Type
					);
				}

				resultItems = ItemSorter.SortAndFilterItems(thread, attempt, takeCount: RECENT_FILTER_ITEM_COUNT);
			} else
				resultItems = ItemSorter.SortAndFilterItems(thread, attempt);

			items.Clear();
			itemToSourceItems.Clear();

			// SortAndFilterItems would have already filtered the favorites out
			// Also, a partitioning method like OrderFavoritesFirst performs better than OrderByDescending
			items.AddRange(resultItems);

			thread.aggregateResults.CopyResultGroupsTo(itemToSourceItems);

			NetHelper.Report(true, "Filtering applied.  Item count: " + items.Count);
		}
	}
}
