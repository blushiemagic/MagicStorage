using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using MagicStorage.UI.States;
using MagicStorage.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.ID;
using MagicStorage.Common;

namespace MagicStorage {
	partial class CraftingGUI {
		internal abstract class CommonCraftingState {
			public static readonly HashSet<int> EmptyGlobalHiddenTypes = new();

			public EnvironmentSandbox sandbox;
			public IEnumerable<Item> heartItems;
			public IEnumerable<Item> simulatorItems;
			public HashSet<int> globalHiddenTypes;
			public ItemTypeOrderedSet hiddenTypes, favoritedTypes;
			public int recipeFilterChoice;

			public bool IsHidden(int item) => globalHiddenTypes.Contains(item) || hiddenTypes.Contains(item);
		}

		private class ThreadState : CommonCraftingState {
			public Recipe[] recipesToRefresh;
			public bool[] recipeConditionsMetSnapshot;
			public string recursionFailReason;
		}

		private static bool currentlyThreading;

		internal static readonly List<Item> items = new();
		internal static readonly Dictionary<int, int> itemCounts = new();
		internal static readonly Dictionary<int, Dictionary<int, int>> itemCountsByPrefix = new();

		// Only used by DoWithdrawResult to check items from modules
		internal static readonly List<Item> sourceItemsFromModules = new();

		internal static int numItemsWithoutSimulators;
		internal static int numSimulatorItems;
		
		[Obsolete("Use MagicUI.RefreshItems() instead", error: true)]
		public static void RefreshItems() => MagicUI.RefreshItems();

		internal static void ResetRefreshCache() {
			recipesToRefresh = null;
		}
		
		internal static void RefreshItems_Inner() {
			Recipe[] toRefresh;
			if (!MagicUI.ForceNextRefreshToBeFull) {
				// Refresh the provided array
				toRefresh = recipesToRefresh;
			} else {
				// Force all recipes to be recalculated
				recipesToRefresh = null;
				toRefresh = null;
			}

			var craftingPage = MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>();

			craftingPage?.RequestThreadWait(waiting: true);

			MagicUI.StopCurrentThread();

			if (!MagicUI.CurrentlyRefreshing) {
				// Inform the UI that a new refresh is about to start so that it can go into a proper "empty" state
				MagicUI.craftingUI?.OnRefreshStart();
			}

			// Always reset the cached values
			ResetRecentRecipeCache();

			lastKnownRecursionErrorForStoredItems = null;

			items.Clear();
			sourceItems.Clear();
			sourceItemsFromModules.Clear();
			numItemsWithoutSimulators = 0;
			TEStorageHeart heart = GetHeart();
			if (heart == null) {
				craftingPage?.RequestThreadWait(waiting: false);

				MagicUI.InvokeOnRefresh();
				return;
			}

			NetHelper.Report(true, "CraftingGUI: RefreshItems invoked");

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			foreach (var module in heart.GetModules())
				module.PreRefreshRecipes(sandbox);

			IEnumerable<Item> heartItems = heart.GetStoredItems();
			IEnumerable<Item> simulatorItems = heart.GetModules().SelectMany(m => m.GetAdditionalItems(sandbox) ?? Array.Empty<Item>())
				.Where(i => i.type > ItemID.None && i.stack > 0)
				.DistinctBy(i => i, ReferenceEqualityComparer.Instance);  //Filter by distinct object references (prevents "duplicate" items from, say, 2 mods adding items from the player's inventory)

			int sortMode = MagicUI.craftingUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.craftingUI.GetPage<FilteringPage>("Filtering").option;

			string searchText = craftingPage.searchBar.State.InputText;

			var globalHiddenRecipes = MagicStorageConfig.GlobalRecipeBlacklist.Where(x => !x.IsUnloaded).Select(x => x.Type).ToHashSet();
			var hiddenRecipes = StoragePlayer.LocalPlayer.HiddenRecipes;
			var favorited = StoragePlayer.LocalPlayer.FavoritedRecipes;

			int recipeChoice = craftingPage.recipeButtons.Choice;
			int modSearchIndex = craftingPage.modSearchBox.ModIndex;

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
					recipesToRefresh = toRefresh,
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

			ExecuteInCraftingGuiEnvironment(() => {
				state.recipeConditionsMetSnapshot = Main.recipe.Take(Recipe.numRecipes).Select(static r => !r.Disabled && RecipeLoader.RecipeAvailable(r)).ToArray();
			});

			if (heart is not null) {
				foreach (EnvironmentModule module in heart.GetModules())
					module.ResetPlayer(sandbox);
			}

			StorageGUI.ThreadContext.Begin(thread);
		}

		private static void SortAndFilter(StorageGUI.ThreadContext thread) {
			currentlyThreading = true;

			if (thread.state is ThreadState state) {
				LoadItemsAndSetDictionaryInfo(thread, state);
				RefreshStorageItems(thread);
				
				try {
					SafelyRefreshRecipes(thread, state);
				} catch when (thread.token.IsCancellationRequested) {
					recipes.Clear();
					recipeAvailable.Clear();
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

			var sandbox = (thread.state as ThreadState).sandbox;

			foreach (var module in thread.heart.GetModules())
				module.PostRefreshRecipes(sandbox);

			if (thread.state is ThreadState state)
				lastKnownRecursionErrorForStoredItems = state.recursionFailReason;

			NetHelper.Report(true, "CraftingGUI: RefreshItems finished");

			MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>()?.RequestThreadWait(waiting: false);
		}

		// Moved to internal method for use by DecraftingGUI
		internal static void LoadItemsAndSetDictionaryInfo(StorageGUI.ThreadContext thread, CommonCraftingState state) {
			try {
				// Task count: loading simulator items, 5 tasks from SortAndFilter, adding full item list, adding module items to source, updating source items list, updating counts dictionary
				thread.InitTaskSchedule(10, "Loading items");

				var clone = thread.Clone(
					newSortMode: SortingOptionLoader.Definitions.ID.Type,
					newFilterMode: FilteringOptionLoader.Definitions.All.Type,
					newSearchText: "",
					newModSearch: ModSearchBox.ModIndexAll);

				thread.context = clone.context = new(state.simulatorItems);

				items.AddRange(ItemSorter.SortAndFilter(clone, aggregate: false));

				thread.CompleteOneTask();

				numSimulatorItems = items.Count;

				var evaluatedSimulatorItems = thread.context.sourceItems;

				// Prepend the heart items before the module items
				NetHelper.Report(true, "Loading stored items from storage system...");

				clone.context = new(state.heartItems);

				var prependedItems = ItemSorter.SortAndFilter(clone).Concat(items).ToList();

				items.Clear();
				items.AddRange(prependedItems);

				thread.CompleteOneTask();

				numItemsWithoutSimulators = items.Count - numSimulatorItems;

				var moduleItems = evaluatedSimulatorItems.ToList();

				sourceItems.AddRange(clone.context.sourceItems.Concat(moduleItems));

				thread.CompleteOneTask();

				sourceItemsFromModules.AddRange(moduleItems.SelectMany(static list => list));

				thread.CompleteOneTask();

				// Context no longer needed
				thread.context = null;

				NetHelper.Report(false, "Total items: " + items.Count);
				NetHelper.Report(false, "Items from modules: " + numSimulatorItems);

				SetCountsDictionaries();

				thread.CompleteOneTask();
			} catch when (thread.token.IsCancellationRequested) {
				items.Clear();
				numItemsWithoutSimulators = 0;
				sourceItems.Clear();
				sourceItemsFromModules.Clear();
				itemCounts.Clear();
				itemCountsByPrefix.Clear();
				throw;
			}
		}

		internal static void SetCountsDictionaries() {
			itemCounts.Clear();
			itemCountsByPrefix.Clear();

			// Previously just used GroupBy, but that doesn't play nice for multiple element data
			foreach (Item item in items) {
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
