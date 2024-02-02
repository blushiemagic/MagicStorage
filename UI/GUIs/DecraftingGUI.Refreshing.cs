using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Components;
using MagicStorage.UI;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class DecraftingGUI {
		private class ThreadState : CraftingGUI.CommonCraftingState {
			public int[] itemsToRefresh;
			public bool[] decraftingRecipeAvailableSnapshot;
			public int[] itemTypeToDecraftRecipeIndexSnapshot;
			public bool[] itemTransmuteAvailableSnapshot;
			public List<IShimmerResultReport> cachedShimmerReports;
		}

		private static bool currentlyThreading;

		public static readonly List<Item> resultItems = new();
		public static readonly List<bool> resultItemsFromModules = new();
		public static readonly List<ItemInfo> resultItemInfo = new();

		internal static void ResetRefreshCache() {
			itemsToRefresh = null;
		}

		internal static void RefreshItems() {
			int[] toRefresh;
			if (!MagicUI.ForceNextRefreshToBeFull) {
				// Refresh the provided set
				toRefresh = itemsToRefresh?.ToArray();
			} else {
				// Force all items to be recalculated
				itemsToRefresh = null;
				toRefresh = null;
			}

			var page = MagicUI.decraftingUI.currentPage as DecraftingUIState.ShimmeringPage;

			page?.RequestThreadWait(waiting: true);

			MagicUI.StopCurrentThread();

			if (!MagicUI.CurrentlyRefreshing) {
				// Inform the UI that a new refresh is about to start so that it can go into a proper "empty" state
				MagicUI.decraftingUI.OnRefreshStart();
			}

			CraftingGUI.items.Clear();
			CraftingGUI.sourceItems.Clear();
			CraftingGUI.sourceItemsFromModules.Clear();
			CraftingGUI.numItemsWithoutSimulators = 0;
			TEStorageHeart heart = GetHeart();
			if (heart == null) {
				ClearAllCollections(unloading: false);

				page?.RequestThreadWait(waiting: false);

				MagicUI.InvokeOnRefresh();
				return;
			}

			NetHelper.Report(true, "DecraftingGUI: RefreshItems invoked");

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			foreach (var module in heart.GetModules())
				module.PreRefreshRecipes(sandbox);

			IEnumerable<Item> heartItems = heart.GetStoredItems();
			IEnumerable<Item> simulatorItems = heart.GetModules().SelectMany(m => m.GetAdditionalItems(sandbox) ?? Array.Empty<Item>())
				.Where(i => i.type > ItemID.None && i.stack > 0)
				.DistinctBy(i => i, ReferenceEqualityComparer.Instance);  //Filter by distinct object references (prevents "duplicate" items from, say, 2 mods adding items from the player's inventory)

			int sortMode = MagicUI.decraftingUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.decraftingUI.GetPage<FilteringPage>("Filtering").option;

			string searchText = page.searchBar.State.InputText;

			var globalHiddenRecipes = MagicStorageConfig.GlobalShimmerItemBlacklist.Where(x => !x.IsUnloaded).Select(x => x.Type).ToHashSet();
			var hiddenRecipes = StoragePlayer.LocalPlayer.HiddenShimmerItems;
			var favorited = StoragePlayer.LocalPlayer.FavoritedShimmerItems;

			int recipeChoice = page.recipeButtons.Choice;
			int modSearchIndex = page.modSearchBox.ModIndex;

			ThreadState state;
			StorageGUI.ThreadContext thread = new(new CancellationTokenSource(), SortAndFilter, AfterSorting) {
				heart = heart,
				sortMode = sortMode,
				filterMode = filterMode,
				searchText = searchText,
				onlyFavorites = false,
				modSearch = modSearchIndex,
				state = state = new ThreadState() {
					sandbox = sandbox,
					itemsToRefresh = toRefresh,
					heartItems = heartItems,
					simulatorItems = simulatorItems,
					globalHiddenTypes = globalHiddenRecipes,
					hiddenTypes = hiddenRecipes,
					favoritedTypes = favorited,
					recipeFilterChoice = recipeChoice
				}
			};

			// Update the adjacent tiles and condition contexts
			AnalyzeIngredients();

			CraftingGUI.ExecuteInCraftingGuiEnvironment(() => {
				IEnumerable<IShimmerResultReport> reports = selectedItem == -1
					? Array.Empty<IShimmerResultReport>()
					: MagicCache.ShimmerInfos[selectedItem].GetShimmerReports();

				state.cachedShimmerReports = reports.Where(static r => r is ItemReport).ToList();  // Ignore any reports that aren't ItemReports, since that's all the result zone cares about

				state.decraftingRecipeAvailableSnapshot = Main.recipe.Take(Recipe.numRecipes).Select(ShimmerMetrics.IsDecraftAvailable).ToArray();
				state.itemTypeToDecraftRecipeIndexSnapshot = ItemID.Sets.Factory.CreateIntSet(-1);
				state.itemTransmuteAvailableSnapshot = ItemID.Sets.Factory.CreateBoolSet(false);

				for (int i = 0; i < ItemLoader.ItemCount; i++) {
					var info = MagicCache.ShimmerInfos[i];
					var attempt = info.GetAttempt(out int decraftingRecipeIndex);

					if (attempt.IsSuccessfulButNotDecraftable())
						state.itemTransmuteAvailableSnapshot[i] = true;
					else if (attempt.IsSuccessful())
						state.itemTypeToDecraftRecipeIndexSnapshot[i] = decraftingRecipeIndex;
				}
			});

			if (heart is not null) {
				foreach (EnvironmentModule module in heart.GetModules())
					module.ResetPlayer(sandbox);
			}

			StorageGUI.ThreadContext.Begin(thread);
		}

		private static void AnalyzeIngredients() {
			NetHelper.Report(true, "Analyzing environment requirements...");

			CraftingGUI.ResetZoneInfo();

			CraftingGUI.AdjustAndAssignZoneInfo();
		}

		private static void SortAndFilter(StorageGUI.ThreadContext thread) {
			currentlyThreading = true;

			if (thread.state is ThreadState state) {
				CraftingGUI.LoadItemsAndSetDictionaryInfo(thread, state);
				RefreshStorageItems(thread);

				try {
					SafelyRefreshItems(thread, state);
				} catch when (thread.token.IsCancellationRequested) {
					viewingItems.Clear();
					itemAvailable.Clear();
					throw;
				}
			}

			currentlyThreading = false;
			ResetRefreshCache();
		}

		private static void AfterSorting(StorageGUI.ThreadContext thread) {
			// Refresh logic in the UIs will only run when this is false
			if (!thread.token.IsCancellationRequested)
				MagicUI.CurrentlyRefreshing = false;

			// Ensure that race conditions with the UI can't occur
			// QueueMainThreadAction will execute the logic in a very specific place
			Main.QueueMainThreadAction(MagicUI.InvokeOnRefresh);

			NetHelper.Report(true, "DecraftingGUI: RefreshItems finished");

			(MagicUI.decraftingUI.currentPage as DecraftingUIState.ShimmeringPage)?.RequestThreadWait(waiting: false);
		}

		private static void RefreshStorageItems(StorageGUI.ThreadContext thread = null) {
			NetHelper.Report(true, "Updating stored ingredients collection and result item...");

			CraftingGUI.storageItems.Clear();
			CraftingGUI.storageItemInfo.Clear();
			CraftingGUI.storageItemsFromModules.Clear();
			resultItems.Clear();
			resultItemInfo.Clear();
			resultItemsFromModules.Clear();

			if (selectedItem <= ItemID.None) {
				thread?.InitAsCompleted("Populating stored ingredients");
				NetHelper.Report(true, "Failed.  No item is selected.");
				return;
			}

			if (thread is not null) {
				if (thread.state is not ThreadState state) {
					thread?.InitAsCompleted("Populating stored ingredients");
					NetHelper.Report(true, "Failed.  Thread state is not valid.");
					return;
				}
			}

			thread?.InitTaskSchedule(CraftingGUI.sourceItems.Count, "Populating stored ingredients");

			int index = 0;

			var resultAggregator = new CraftingGUI.StoredItemAggregator(resultItems, resultItemsFromModules, resultItemInfo);

			foreach (List<Item> itemsFromSource in CraftingGUI.sourceItems) {
				foreach (Item item in itemsFromSource) {
					bool b = false;
					ref bool added = ref b;
					CraftingGUI.CheckItemFromSource(null, item, index, ref added, IsItemValidForStorage);

					added = false;
					CraftingGUI.CheckItemFromSource(resultAggregator, item, index, ref added, IsItemValidForResult);
				}

				index++;

				thread?.CompleteOneTask();
			}
		}

		private static bool IsItemValidForStorage(Item item) => item.type == selectedItem && item.stack > 0;

		private static bool IsItemValidForResult(Item item) {
			if (selectedItem == -1)
				return false;

			IShimmerResultReport report = new ItemReport(item.type);
			if (currentlyThreading) {
				if (MagicUI.activeThread.state is not ThreadState state)
					return false;

				foreach (var cachedReport in state.cachedShimmerReports) {
					if (cachedReport.Equals(report))
						return true;
				}

				return false;
			}

			// Need to check the reports manually
			foreach (var cachedReport in MagicCache.ShimmerInfos[selectedItem].GetShimmerReports().Where(static r => r is ItemReport)) {
				if (cachedReport.Equals(report))
					return true;
			}

			return false;
		}
	}
}
