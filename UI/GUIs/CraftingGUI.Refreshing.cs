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

namespace MagicStorage {
	partial class CraftingGUI {
		private class ThreadState {
			public EnvironmentSandbox sandbox;
			public Recipe[] recipesToRefresh;
			public IEnumerable<Item> heartItems;
			public IEnumerable<Item> simulatorItems;
			public ItemTypeOrderedSet hiddenRecipes, favoritedRecipes;
			public int recipeFilterChoice;
			public bool[] recipeConditionsMetSnapshot;
		}

		private static bool currentlyThreading;

		internal static readonly List<Item> items = new();
		private static readonly Dictionary<int, int> itemCounts = new();

		// Only used by DoWithdrawResult to check items from modules
		private static readonly List<Item> sourceItemsFromModules = new();

		private static int numItemsWithoutSimulators;
		private static int numSimulatorItems;
		
		public static void RefreshItems() => RefreshItemsAndSpecificRecipes(null);

		private static void RefreshItemsAndSpecificRecipes(Recipe[] toRefresh) {
			if (!StorageGUI.ForceNextRefreshToBeFull) {
				// Custom array provided?  Refresh the default array anyway
				SetNextDefaultRecipeCollectionToRefresh(toRefresh);
				toRefresh = recipesToRefresh;
			} else {
				// Force all recipes to be recalculated
				recipesToRefresh = null;
				toRefresh = null;
			}

			var craftingPage = MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting");

			craftingPage?.RequestThreadWait(waiting: true);

			if (StorageGUI.CurrentlyRefreshing) {
				StorageGUI.activeThread?.Stop();
				StorageGUI.activeThread = null;
			}

			// Always reset the cached values
			ResetRecentRecipeCache();

			items.Clear();
			sourceItems.Clear();
			sourceItemsFromModules.Clear();
			numItemsWithoutSimulators = 0;
			TEStorageHeart heart = GetHeart();
			if (heart == null) {
				craftingPage?.RequestThreadWait(waiting: false);

				StorageGUI.InvokeOnRefresh();
				return;
			}

			NetHelper.Report(true, "CraftingGUI: RefreshItemsAndSpecificRecipes invoked");

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			foreach (var module in heart.GetModules())
				module.PreRefreshRecipes(sandbox);

			StorageGUI.CurrentlyRefreshing = true;

			IEnumerable<Item> heartItems = heart.GetStoredItems();
			IEnumerable<Item> simulatorItems = heart.GetModules().SelectMany(m => m.GetAdditionalItems(sandbox) ?? Array.Empty<Item>())
				.Where(i => i.type > ItemID.None && i.stack > 0)
				.DistinctBy(i => i, ReferenceEqualityComparer.Instance);  //Filter by distinct object references (prevents "duplicate" items from, say, 2 mods adding items from the player's inventory)

			int sortMode = MagicUI.craftingUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.craftingUI.GetPage<FilteringPage>("Filtering").option;

			var recipesPage = MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting");
			string searchText = recipesPage.searchBar.Text;

			var hiddenRecipes = StoragePlayer.LocalPlayer.HiddenRecipes;
			var favorited = StoragePlayer.LocalPlayer.FavoritedRecipes;

			int recipeChoice = recipesPage.recipeButtons.Choice;
			int modSearchIndex = recipesPage.modSearchBox.ModIndex;

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
					hiddenRecipes = hiddenRecipes,
					favoritedRecipes = favorited,
					recipeFilterChoice = recipeChoice
				}
			};

			// Update the adjacent tiles and condition contexts
			AnalyzeIngredients();

			ExecuteInCraftingGuiEnvironment(() => {
				state.recipeConditionsMetSnapshot = Main.recipe.Take(Recipe.maxRecipes).Select(static r => !r.Disabled && RecipeLoader.RecipeAvailable(r)).ToArray();
			});

			if (heart is not null) {
				foreach (EnvironmentModule module in heart.GetModules())
					module.ResetPlayer(sandbox);
			}

			StorageGUI.ThreadContext.Begin(thread);
		}

		private static void SortAndFilter(StorageGUI.ThreadContext thread) {
			currentlyThreading = true;

			currentRecipeIsAvailable = null;

			if (thread.state is ThreadState state) {
				LoadStoredItems(thread, state);
				RefreshStorageItems();
				
				try {
					SafelyRefreshRecipes(thread, state);
				} catch when (thread.token.IsCancellationRequested) {
					recipes.Clear();
					recipeAvailable.Clear();
					throw;
				}
			}

			currentlyThreading = false;
			recipesToRefresh = null;
		}

		private static void AfterSorting(StorageGUI.ThreadContext thread) {
			// Refresh logic in the UIs will only run when this is false
			if (!thread.token.IsCancellationRequested)
				StorageGUI.CurrentlyRefreshing = false;

			// Ensure that race conditions with the UI can't occur
			// QueueMainThreadAction will execute the logic in a very specific place
			Main.QueueMainThreadAction(StorageGUI.InvokeOnRefresh);

			var sandbox = (thread.state as ThreadState).sandbox;

			foreach (var module in thread.heart.GetModules())
				module.PostRefreshRecipes(sandbox);

			NetHelper.Report(true, "CraftingGUI: RefreshItemsAndSpecificRecipes finished");

			MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting")?.RequestThreadWait(waiting: false);
		}

		private static void LoadStoredItems(StorageGUI.ThreadContext thread, ThreadState state) {
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

				var simulatorItems = thread.context.sourceItems;

				// Prepend the heart items before the module items
				NetHelper.Report(true, "Loading stored items from storage system...");

				clone.context = new(state.heartItems);

				var prependedItems = ItemSorter.SortAndFilter(clone).Concat(items).ToList();

				items.Clear();
				items.AddRange(prependedItems);

				thread.CompleteOneTask();

				numItemsWithoutSimulators = items.Count - numSimulatorItems;

				var moduleItems = simulatorItems.ToList();

				sourceItems.AddRange(clone.context.sourceItems.Concat(moduleItems));

				thread.CompleteOneTask();

				sourceItemsFromModules.AddRange(moduleItems.SelectMany(static list => list));

				thread.CompleteOneTask();

				// Context no longer needed
				thread.context = null;

				NetHelper.Report(false, "Total items: " + items.Count);
				NetHelper.Report(false, "Items from modules: " + numSimulatorItems);

				itemCounts.Clear();
				foreach ((int type, int amount) in items.GroupBy(item => item.type, item => item.stack, (type, stacks) => (type, stacks.ConstrainedSum())))
					itemCounts[type] = amount;

				thread.CompleteOneTask();
			} catch when (thread.token.IsCancellationRequested) {
				items.Clear();
				numItemsWithoutSimulators = 0;
				sourceItems.Clear();
				sourceItemsFromModules.Clear();
				itemCounts.Clear();
				throw;
			}
		}
	}
}
