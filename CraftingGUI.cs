using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Items;
using MagicStorage.Sorting;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	public static class CraftingGUI
	{
		public const int RecipeButtonsAvailableChoice = 0;
		//Button location could either be the third (2) or fourth (3) option depending on if the favoriting config is enabled
		public static int RecipeButtonsBlacklistChoice => MagicStorageConfig.CraftingFavoritingEnabled ? 3 : 2;
		public const int RecipeButtonsFavoritesChoice = 2;
		public const int Padding = 4;
		public const int RecipeColumns = 10;
		public const int IngredientColumns = 7;
		public const float InventoryScale = 0.85f;
		public const float SmallScale = 0.7f;
		public const int StartMaxCraftTimer = 20;
		public const int StartMaxRightClickTimer = 20;
		public const float ScrollBar2ViewSize = 1f;
		public const float RecipeScrollBarViewSize = 1f;

		internal static readonly List<Item> items = new();

		private static readonly Dictionary<int, int> itemCounts = new();
		internal static List<Recipe> recipes = new();
		internal static List<bool> recipeAvailable = new();
		internal static Recipe selectedRecipe;

		internal static bool slotFocus;

		internal static readonly List<Item> storageItems = new();
		internal static readonly List<bool> storageItemsFromModules = new();
		internal static readonly List<ItemData> blockStorageItems = new();
		internal static readonly List<List<Item>> sourceItems = new();

		public static int craftAmountTarget;

		private static Item result;
		internal static int craftTimer;
		internal static int maxCraftTimer = StartMaxCraftTimer;
		internal static int rightClickTimer;

		internal static int maxRightClickTimer = StartMaxRightClickTimer;

		public static bool CatchDroppedItems;
		public static List<Item> DroppedItems = new();

		private static bool[] adjTiles = new bool[TileLoader.TileCount];
		private static bool adjWater;
		private static bool adjLava;
		private static bool adjHoney;
		private static bool zoneSnow;
		private static bool alchemyTable;
		private static bool graveyard;
		public static bool Campfire { get; private set; }

		internal static void Unload()
		{
			selectedRecipe = null;
		}

		internal static void Reset() {
			Campfire = false;
			craftTimer = 0;
			maxCraftTimer = StartMaxCraftTimer;
			craftAmountTarget = 1;
		}

		internal static Item GetStation(int slot, ref int context)
		{
			List<Item> stations = GetCraftingStations();
			if (stations is not null && slot < stations.Count)
				return stations[slot];
			return new Item();
		}

		internal static Item GetHeader(int slot, ref int context)
		{
			return selectedRecipe?.createItem ?? new Item();
		}

		internal static Item GetIngredient(int slot, ref int context)
		{
			if (selectedRecipe == null || slot >= selectedRecipe.requiredItem.Count)
				return new Item();

			Item item = selectedRecipe.requiredItem[slot].Clone();
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Wood) && item.type == ItemID.Wood)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.Wood));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Sand) && item.type == ItemID.SandBlock)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.SandBlock));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.IronBar) && item.type == ItemID.IronBar)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.IronBar));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Fragment) && item.type == ItemID.FragmentSolar)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Language.GetText("LegacyMisc.51").Value);
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.PressurePlate) && item.type == ItemID.GrayPressurePlate)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Language.GetText("LegacyMisc.38").Value);
			if (ProcessGroupsForText(selectedRecipe, item.type, out string nameOverride))
				item.SetNameOverride(nameOverride);

			int totalGroupStack = 0;
			Item storageItem = storageItems.FirstOrDefault(i => i.type == item.type) ?? new Item();

			foreach (RecipeGroup rec in selectedRecipe.acceptedGroups.Select(index => RecipeGroup.recipeGroups[index])) {
				if (rec.ValidItems.Contains(item.type)) {
					foreach (int type in rec.ValidItems)
						totalGroupStack += storageItems.Where(i => i.type == type).Sum(i => i.stack);
				}
			}

			if (!item.IsAir) {
				if (storageItem.IsAir && totalGroupStack == 0)
					context = ItemSlot.Context.ChestItem;  // Unavailable - Red
				else if (storageItem.stack < item.stack && totalGroupStack < item.stack)
					context = ItemSlot.Context.BankItem;  // Partially in stock - Pinkish

				// context == 0 - Available - Default Blue
				if (context != 0) {
					bool craftable;

					using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable))
						craftable = MagicCache.ResultToRecipe.TryGetValue(item.type, out var r) && r.Any(recipe => IsAvailable(recipe));

					if (craftable)
						context = ItemSlot.Context.TrashItem;  // Craftable - Light green
				}
			}

			return item;
		}

		internal static bool ProcessGroupsForText(Recipe recipe, int type, out string theText)
		{
			foreach (int num in recipe.acceptedGroups)
				if (RecipeGroup.recipeGroups[num].ContainsItem(type))
				{
					theText = RecipeGroup.recipeGroups[num].GetText();
					return true;
				}

			theText = "";
			return false;
		}

		private static int? amountCraftableForCurrentRecipe;
		private static Recipe recentRecipeAmountCraftable;

		public static int AmountCraftableForCurrentRecipe() {
			if (currentlyThreading || StorageGUI.CurrentlyRefreshing)
				return 0;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeAmountCraftable, selectedRecipe) && amountCraftableForCurrentRecipe is { } amount)
				return amount;

			// Calculate the value
			recentRecipeAmountCraftable = selectedRecipe;
			amountCraftableForCurrentRecipe = amount = AmountCraftable(selectedRecipe);
			return amount;
		}

		internal static bool requestingAmountFromUI;

		// Calculates how many times a recipe can be crafted using available items
		internal static int AmountCraftable(Recipe recipe)
		{
			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				// Clone the available inventory
				Dictionary<int, int> availableInventory = new Dictionary<int, int>(itemCounts);

				using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI))
					return recursiveRecipe.GetMaxCraftable(availableInventory);
			}

			// Handle the old logic
			if (!IsAvailable(recipe))
				return 0;

			// Local capturing
			Recipe r = recipe;

			int GetMaxCraftsAmount(Item requiredItem) {
				int total = 0;
				foreach (Item inventoryItem in items) {
					if (inventoryItem.type == requiredItem.type || RecipeGroupMatch(r, inventoryItem.type, requiredItem.type))
						total += inventoryItem.stack;
				}

				int craftable = total / requiredItem.stack;
				return craftable;
			}

			int maxCrafts = recipe.requiredItem.Select(GetMaxCraftsAmount).Prepend(int.MaxValue).Min();

			return maxCrafts * recipe.createItem.stack;
		}

		internal static Item GetResult(int slot, ref int context) => slot == 0 && result is not null ? result : new Item();

		internal static void ClickCraftButton(ref bool stillCrafting) {
			if (craftTimer <= 0)
			{
				craftTimer = maxCraftTimer;
				maxCraftTimer = maxCraftTimer * 3 / 4;
				if (maxCraftTimer <= 0)
					maxCraftTimer = 1;

				int amount = craftAmountTarget;

				if (MagicStorageConfig.UseOldCraftMenu && Main.keyState.IsKeyDown(Keys.LeftControl))
					amount = int.MaxValue;

				Craft(amount);

				IEnumerable<int> allItemTypes = selectedRecipe.requiredItem.Select(i => i.type).Prepend(selectedRecipe.createItem.type);

				//If no recipes were affected, that's fine, none of the recipes will be touched due to the calulated Recipe array being empty
				SetNextDefaultRecipeCollectionToRefresh(allItemTypes);
				StorageGUI.SetRefresh();
				SoundEngine.PlaySound(SoundID.Grab);
			}

			craftTimer--;
			stillCrafting = true;
		}

		internal static void ClickAmountButton(int amount, bool offset) {
			if (StorageGUI.CurrentlyRefreshing)
				return;  // Do not read anything until refreshing is completed

			if (offset && (amount == 1 || craftAmountTarget > 1))
				craftAmountTarget += amount;
			else
				craftAmountTarget = amount;  //Snap directly to the amount if the amount target was 1 (this makes clicking 10 when at 1 just go to 10 instead of 11)

			ClampCraftAmount();

			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		internal static void ClampCraftAmount() {
			if (craftAmountTarget < 1)
				craftAmountTarget = 1;
			else if (!IsCurrentRecipeFullyAvailable())
				craftAmountTarget = 1;
			else
			{
				int amountCraftable = AmountCraftableForCurrentRecipe();
				int max = Math.Min(amountCraftable, selectedRecipe.createItem.maxStack);

				if (craftAmountTarget > max)
					craftAmountTarget = max;
			}
		}

		internal static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		internal static TECraftingAccess GetCraftingEntity() => StoragePlayer.LocalPlayer.GetCraftingAccess();

		internal static List<Item> GetCraftingStations() => GetCraftingEntity()?.stations ?? new();

		public static void RefreshItems() => RefreshItemsAndSpecificRecipes(null);

		private static int numItemsWithoutSimulators;
		private static int numSimulatorItems;

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
		private static Recipe[] recipesToRefresh;

		/// <summary>
		/// Adds <paramref name="recipes"/> to the collection of recipes to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="recipes">An array of recipes to update.  If <see langword="null"/>, then nothing happens</param>
		public static void SetNextDefaultRecipeCollectionToRefresh(Recipe[] recipes) {
			if (recipesToRefresh is null) {
				if (recipes is not null)
					NetHelper.Report(true, $"Setting next refresh to check {recipes.Length} recipes");

				recipesToRefresh = recipes;
				return;
			}

			if (recipes is null)
				return;

			recipesToRefresh = recipesToRefresh.Concat(recipes).DistinctBy(static r => r, ReferenceEqualityComparer.Instance).ToArray();

			NetHelper.Report(true, $"Setting next refresh to check {recipes.Length} recipes");
		}

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedItemType"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemType">The item type to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(int affectedItemType) {
			SetNextDefaultRecipeCollectionToRefresh(MagicCache.RecipesUsingItemType.TryGetValue(affectedItemType, out var result) ? result.Value : null);
		}

		/// <summary>
		/// Adds all recipes which use the IDs in <paramref name="affectedItemTypes"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemTypes">A collection of item types to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(IEnumerable<int> affectedItemTypes) {
			SetNextDefaultRecipeCollectionToRefresh(affectedItemTypes.SelectMany(static i => MagicCache.RecipesUsingItemType.TryGetValue(i, out var result) ? result.Value : Array.Empty<Recipe>())
				.DistinctBy(static r => r, ReferenceEqualityComparer.Instance)
				.ToArray());
		}

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedTileType"/> as a required tile to the collection of recipes to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileType">The tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(int affectedTileType) {
			SetNextDefaultRecipeCollectionToRefresh(MagicCache.RecipesUsingTileType.TryGetValue(affectedTileType, out var result) ? result.Value : null);
		}

		/// <summary>
		/// Adds all recipes which the IDs in <paramref name="affectedTileTypes"/> as a required tile to the collection of recipes to refresh when calling <see cref="RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileTypes">A collection of the tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(IEnumerable<int> affectedTileTypes) {
			SetNextDefaultRecipeCollectionToRefresh(affectedTileTypes.SelectMany(static t => MagicCache.RecipesUsingTileType.TryGetValue(t, out var result) ? result.Value : Array.Empty<Recipe>())
				.DistinctBy(static r => r, ReferenceEqualityComparer.Instance)
				.ToArray());
		}

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

			// Unlike in StorageGUI, items need to be read ASAP due to EnvironmentModule adding them
			var clone = thread.Clone(
				newSortMode: SortingOptionLoader.Definitions.ID.Type,
				newFilterMode: FilteringOptionLoader.Definitions.All.Type,
				newSearchText: "",
				newModSearch: ModSearchBox.ModIndexAll);

			thread.context = clone.context = new(state.simulatorItems);

			items.AddRange(ItemSorter.SortAndFilter(clone, aggregate: false));

			numSimulatorItems = items.Count;

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
				var simulatorItems = thread.context.sourceItems;

				// Prepend the heart items before the module items
				var clone = thread.Clone(
					newSortMode: SortingOptionLoader.Definitions.ID.Type,
					newFilterMode: FilteringOptionLoader.Definitions.All.Type,
					newSearchText: "",
					newModSearch: ModSearchBox.ModIndexAll);

				NetHelper.Report(true, "Loading stored items from storage system...");

				clone.context = new(state.heartItems);

				var prependedItems = ItemSorter.SortAndFilter(clone).Concat(items).ToList();

				items.Clear();
				items.AddRange(prependedItems);

				numItemsWithoutSimulators = items.Count - numSimulatorItems;

				sourceItems.AddRange(clone.context.sourceItems.Concat(simulatorItems));

				// Context no longer needed
				thread.context = null;

				NetHelper.Report(false, "Total items: " + items.Count);
				NetHelper.Report(false, "Items from modules: " + numSimulatorItems);

				itemCounts.Clear();
				foreach ((int type, int amount) in items.GroupBy(item => item.type, item => item.stack, (type, stacks) => (type, stacks.ConstrainedSum())))
					itemCounts[type] = amount;
			} catch when (thread.token.IsCancellationRequested) {
				items.Clear();
				numItemsWithoutSimulators = 0;
				sourceItems.Clear();
				itemCounts.Clear();
				throw;
			}
		}

		private static void SafelyRefreshRecipes(StorageGUI.ThreadContext thread, ThreadState state) {
			try {
				NetHelper.Report(false, "Visible recipes: " + recipes.Count);
				NetHelper.Report(false, "Available recipes: " + recipeAvailable.Count(static b => b));

				if (state.recipesToRefresh is null)
					RefreshRecipes(thread, state);  //Refresh all recipes
				else
					RefreshSpecificRecipes(thread, state);

				NetHelper.Report(true, "Recipe refreshing finished");
			} catch (Exception e) {
				Main.NewTextMultiline(e.ToString(), c: Color.White);
			}
		}

		private static void RefreshRecipes(StorageGUI.ThreadContext thread, ThreadState state)
		{
			NetHelper.Report(true, "Refreshing all recipes");

			DoFiltering(thread, state);

			bool didDefault = false;

			// now if nothing found we disable filters one by one
			if (thread.searchText.Length > 0)
			{
				if (recipes.Count == 0 && state.hiddenRecipes.Count > 0)
				{
					NetHelper.Report(true, "No recipes passed the filter.  Attempting filter with no hidden recipes");

					// search hidden recipes too
					state.hiddenRecipes = ItemTypeOrderedSet.Empty;

					MagicUI.lastKnownSearchBarErrorReason = Language.GetTextValue("Mods.MagicStorage.Warnings.CraftingNoBlacklist");
					didDefault = true;

					DoFiltering(thread, state);
				}

				/*
				if (recipes.Count == 0 && filterMode != FilterMode.All)
				{
					// any category
					filterMode = FilterMode.All;
					DoFiltering(sortMode, filterMode, hiddenRecipes, favorited);
				}
				*/

				if (recipes.Count == 0 && thread.modSearch != ModSearchBox.ModIndexAll)
				{
					NetHelper.Report(true, "No recipes passed the filter.  Attempting filter with All Mods setting");

					// search all mods
					thread.modSearch = ModSearchBox.ModIndexAll;

					MagicUI.lastKnownSearchBarErrorReason = Language.GetTextValue("Mods.MagicStorage.Warnings.CraftingDefaultToAllMods");
					didDefault = true;

					DoFiltering(thread, state);
				}
			}

			if (!didDefault)
				MagicUI.lastKnownSearchBarErrorReason = null;
		}

		private static void DoFiltering(StorageGUI.ThreadContext thread, ThreadState state)
		{
			try {
				NetHelper.Report(true, "Retrieving recipes...");

				var filteredRecipes = ItemSorter.GetRecipes(thread);

				NetHelper.Report(true, "Sorting recipes...");

				IEnumerable<Recipe> sortedRecipes = SortRecipes(thread, state, filteredRecipes);

				recipes.Clear();
				recipeAvailable.Clear();

				using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable)) {
					if (state.recipeFilterChoice == RecipeButtonsAvailableChoice)
					{
						NetHelper.Report(true, "Filtering out only available recipes...");

						recipes.AddRange(sortedRecipes.Where(r => IsAvailable(r)));
						recipeAvailable.AddRange(Enumerable.Repeat(true, recipes.Count));
					}
					else
					{
						recipes.AddRange(sortedRecipes);
						recipeAvailable.AddRange(recipes.AsParallel().AsOrdered().Select(r => IsAvailable(r)));
					}
				}
			} catch when (thread.token.IsCancellationRequested) {
				recipes.Clear();
				recipeAvailable.Clear();
			}
		}

		private static void RefreshSpecificRecipes(StorageGUI.ThreadContext thread, ThreadState state) {
			NetHelper.Report(true, "Refreshing " + state.recipesToRefresh.Length + " recipes");

			//Assumes that the recipes are visible in the GUI
			bool needsResort = false;

			foreach (Recipe recipe in state.recipesToRefresh) {
				Recipe orig = recipe;
				Recipe check = recipe;

				if (check is null)
					continue;

				int index = recipes.IndexOf(check);  // TODO: check.RecipeIndex?

				using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable)) {
					if (!IsAvailable(check)) {
						if (index >= 0) {
							if (state.recipeFilterChoice == RecipeButtonsAvailableChoice) {
								//Remove the recipe
								recipes.RemoveAt(index);
								recipeAvailable.RemoveAt(index);
							} else {
								//Simply mark the recipe as unavailable
								recipeAvailable[index] = false;
							}
						}
					} else {
						if (state.recipeFilterChoice == RecipeButtonsAvailableChoice) {
							if (index < 0 && CanBeAdded(thread, state, orig)) {
								//Add the recipe
								recipes.Add(orig);
								needsResort = true;
							}
						} else {
							if (index >= 0) {
								//Simply mark the recipe as available
								recipeAvailable[index] = true;
							}
						}
					}
				}
			}

			if (needsResort) {
				var sorted = new List<Recipe>(recipes)
					.AsParallel()
					.AsOrdered();

				IEnumerable<Recipe> sortedRecipes = SortRecipes(thread, state, sorted);

				recipes.Clear();
				recipeAvailable.Clear();

				recipes.AddRange(sortedRecipes);
				recipeAvailable.AddRange(Enumerable.Repeat(true, recipes.Count));
			}
		}

		private static bool CanBeAdded(StorageGUI.ThreadContext thread, ThreadState state, Recipe r) => Array.IndexOf(MagicCache.FilteredRecipesCache[thread.filterMode], r) >= 0
			&& ItemSorter.FilterBySearchText(r.createItem, thread.searchText, thread.modSearch)
			// show only blacklisted recipes only if choice = 2, otherwise show all other
			&& (!MagicStorageConfig.RecipeBlacklistEnabled || state.recipeFilterChoice == RecipeButtonsBlacklistChoice == state.hiddenRecipes.Contains(r.createItem))
			// show only favorited items if selected
			&& (!MagicStorageConfig.CraftingFavoritingEnabled || state.recipeFilterChoice != RecipeButtonsFavoritesChoice || state.favoritedRecipes.Contains(r.createItem));

		private static IEnumerable<Recipe> SortRecipes(StorageGUI.ThreadContext thread, ThreadState state, IEnumerable<Recipe> source) {
			IEnumerable<Recipe> sortedRecipes = ItemSorter.DoSorting(thread, source, r => r.createItem);

			// show only blacklisted recipes only if choice = 2, otherwise show all other
			if (MagicStorageConfig.RecipeBlacklistEnabled)
				sortedRecipes = sortedRecipes.Where(x => state.recipeFilterChoice == RecipeButtonsBlacklistChoice == state.hiddenRecipes.Contains(x.createItem));

			// favorites first
			if (MagicStorageConfig.CraftingFavoritingEnabled) {
				sortedRecipes = sortedRecipes.Where(x => state.recipeFilterChoice != RecipeButtonsFavoritesChoice || state.favoritedRecipes.Contains(x.createItem));
					
				sortedRecipes = sortedRecipes.OrderByDescending(r => state.favoritedRecipes.Contains(r.createItem) ? 1 : 0);
			}

			return sortedRecipes;
		}

		private static void AnalyzeIngredients()
		{
			NetHelper.Report(true, "Analyzing crafting stations and environment requirements...");

			Player player = Main.LocalPlayer;
			if (adjTiles.Length != player.adjTile.Length)
				Array.Resize(ref adjTiles, player.adjTile.Length);

			Array.Clear(adjTiles, 0, adjTiles.Length);
			adjWater = false;
			adjLava = false;
			adjHoney = false;
			zoneSnow = false;
			alchemyTable = false;
			graveyard = false;
			Campfire = false;

			foreach (Item item in GetCraftingStations())
			{
				if (item.IsAir)
					continue;

				if (item.createTile >= TileID.Dirt)
				{
					adjTiles[item.createTile] = true;
					switch (item.createTile)
					{
						case TileID.GlassKiln:
						case TileID.Hellforge:
							adjTiles[TileID.Furnaces] = true;
							break;
						case TileID.AdamantiteForge:
							adjTiles[TileID.Furnaces] = true;
							adjTiles[TileID.Hellforge] = true;
							break;
						case TileID.MythrilAnvil:
							adjTiles[TileID.Anvils] = true;
							break;
						case TileID.BewitchingTable:
						case TileID.Tables2:
							adjTiles[TileID.Tables] = true;
							break;
						case TileID.AlchemyTable:
							adjTiles[TileID.Bottles] = true;
							adjTiles[TileID.Tables] = true;
							alchemyTable = true;
							break;
					}

					if (item.createTile == TileID.Tombstones)
					{
						adjTiles[TileID.Tombstones] = true;
						graveyard = true;
					}

					bool[] oldAdjTile = (bool[])player.adjTile.Clone();
					bool oldAdjWater = adjWater;
					bool oldAdjLava = adjLava;
					bool oldAdjHoney = adjHoney;
					bool oldAlchemyTable = alchemyTable;
					player.adjTile = adjTiles;
					player.adjWater = false;
					player.adjLava = false;
					player.adjHoney = false;
					player.alchemyTable = false;

					TileLoader.AdjTiles(player, item.createTile);

					if (player.adjTile[TileID.WorkBenches] || player.adjTile[TileID.Tables] || player.adjTile[TileID.Tables2])
						player.adjTile[TileID.Chairs] = true;
					if (player.adjWater || TileID.Sets.CountsAsWaterSource[item.createTile])
						adjWater = true;
					if (player.adjLava || TileID.Sets.CountsAsLavaSource[item.createTile])
						adjLava = true;
					if (player.adjHoney || TileID.Sets.CountsAsHoneySource[item.createTile])
						adjHoney = true;
					if (player.alchemyTable || player.adjTile[TileID.AlchemyTable])
						alchemyTable = true;
					if (player.adjTile[TileID.Tombstones])
						graveyard = true;

					player.adjTile = oldAdjTile;
					player.adjWater = oldAdjWater;
					player.adjLava = oldAdjLava;
					player.adjHoney = oldAdjHoney;
					player.alchemyTable = oldAlchemyTable;
				}

				switch (item.type)
				{
					case ItemID.WaterBucket:
					case ItemID.BottomlessBucket:
						adjWater = true;
						break;
					case ItemID.LavaBucket:
					case ItemID.BottomlessLavaBucket:
						adjLava = true;
						break;
					case ItemID.HoneyBucket:
						adjHoney = true;
						break;
				}
				if (item.type == ModContent.ItemType<SnowBiomeEmulator>())
				{
					zoneSnow = true;
				}

				if (item.type == ModContent.ItemType<BiomeGlobe>())
				{
					zoneSnow = true;
					graveyard = true;
					Campfire = true;
					adjWater = true;
					adjLava = true;
					adjHoney = true;

					adjTiles[TileID.Campfire] = true;
					adjTiles[TileID.DemonAltar] = true;
				}
			}

			adjTiles[ModContent.TileType<Components.CraftingAccess>()] = true;

			TEStorageHeart heart = GetHeart();
			EnvironmentSandbox sandbox = new(player, heart);
			CraftingInformation information = new(Campfire, zoneSnow, graveyard, adjWater, adjLava, adjHoney, alchemyTable, adjTiles);

			if (heart is not null) {
				foreach (EnvironmentModule module in heart.GetModules())
					module.ModifyCraftingZones(sandbox, ref information);
			}

			Campfire = information.campfire;
			zoneSnow = information.snow;
			graveyard = information.graveyard;
			adjWater = information.water;
			adjLava = information.lava;
			adjHoney = information.honey;
			alchemyTable = information.alchemyTable;
			adjTiles = information.adjTiles;
		}

		private static bool? currentRecipeIsAvailable;
		private static Recipe recentRecipeAvailable;

		/// <summary>
		/// Returns <see langword="true"/> if the current recipe is available and passes the "blocked ingredients" filter
		/// </summary>
		public static bool IsCurrentRecipeFullyAvailable() {
			if (currentlyThreading || StorageGUI.CurrentlyRefreshing)
				return false;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeAvailable, selectedRecipe) && currentRecipeIsAvailable is { } available)
				return available;

			// Calculate the value
			recentRecipeAvailable = selectedRecipe;
			currentRecipeIsAvailable = available = IsAvailable(selectedRecipe) && PassesBlock(selectedRecipe);
			return available;
		}

		public static void ResetRecentRecipeCache() {
			recentRecipeAvailable = null;
			currentRecipeIsAvailable = null;
			recentRecipeAmountCraftable = null;
			amountCraftableForCurrentRecipe = null;
		}

		/// <summary>
		/// Returns the recursion crafting tree for <paramref name="recipe"/> if it exists and recursion is enabled, or <see langword="null"/> otherwise.
		/// </summary>
		/// <param name="recipe">The recipe</param>
		/// <param name="toCraft">The quantity of the final recipe's crafted item to create</param>
		public static OrderedRecipeTree GetCraftingTree(Recipe recipe, int toCraft = 1) {
			if (!MagicStorageConfig.IsRecursionEnabled || !recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe))
				return null;

			return recursiveRecipe.GetCraftingTree(toCraft, availableInventory: itemCounts);
		}

		internal static bool disableNetPrintingForIsAvailable;

		public static bool IsAvailable(Recipe recipe, bool checkRecursive = true)
		{
			if (recipe is null)
				return false;

			if (!disableNetPrintingForIsAvailable) {
				NetHelper.Report(true, "Checking if recipe is available...");

				if (checkRecursive)
					NetHelper.Report(false, "Calculating recursion tree for recipe...");
			}

			bool available;
			if (checkRecursive && GetCraftingTree(recipe) is OrderedRecipeTree craftingTree) {
				// Clone the item counts so that the inventory can be faked
				isAvailable_ItemCountsDictionary = new Dictionary<int, int>(itemCounts);

				craftingTree.TrimBranches(IsAvailable_GetItemCount);

				// Put all remaining recipes on a stack, and then process them in that order
				// If any recipe ends up not being fulfilled, the original recipe will not be available
				Stack<OrderedRecipeContext> recipeStack = craftingTree.GetProcessingOrder();

				// Check each recipe, then update item count dictionary with its ingredients and result
				var heart = GetHeart();
				EnvironmentSandbox sandbox = new EnvironmentSandbox(Main.LocalPlayer, heart);
				IEnumerable<EnvironmentModule> modules = heart.GetModules();

				if (!disableNetPrintingForIsAvailable)
					NetHelper.Report(true, "Processing recipe order...");

				available = true;
				foreach (OrderedRecipeContext context in recipeStack) {
					// Trimmed branch?  Ignore, assume available
					if (context.amountToCraft <= 0)
						continue;

					if (!IsAvailable_CheckRecipe(context.recipe)) {
						available = false;
						break;
					}

					int batches = (int)Math.Ceiling(context.amountToCraft / (double)context.recipe.createItem.stack);

					// Remove the required items from the inventory
					IsAvailable_ConsumeFakeCounts(context, batches, out Dictionary<int, int> consumedItemCounts);

					// Fake the crafts
					CatchDroppedItems = true;
					DroppedItems.Clear();

					List<Item> consumedItems = consumedItemCounts.Select(kvp => new Item(kvp.Key, kvp.Value)).ToList();

					Item createItem = context.recipe.createItem;
					Item clonedItem = createItem.Clone();

					for (int i = 0; i < batches; i++) {
						RecipeLoader.OnCraft(clonedItem, context.recipe, consumedItems, new Item());

						foreach (EnvironmentModule module in modules)
							module.OnConsumeItemsForRecipe(sandbox, context.recipe, consumedItems);
					}

					CatchDroppedItems = false;

					// Add the "results" and extra items to the inventory
					isAvailable_ItemCountsDictionary.AddOrSumCount(createItem.type, createItem.stack * batches);

					foreach (Item droppedItem in DroppedItems)
						isAvailable_ItemCountsDictionary.AddOrSumCount(droppedItem.type, droppedItem.stack);
				}
			} else {
				isAvailable_ItemCountsDictionary = itemCounts;
				available = IsAvailable_CheckRecipe(recipe);
				isAvailable_ItemCountsDictionary = null;
			}

			if (!disableNetPrintingForIsAvailable)
				NetHelper.Report(true, $"Recipe {(available ? "was" : "was not")} available");

			return available;
		}

		private static void IsAvailable_ConsumeFakeCounts(OrderedRecipeContext context, int batches, out Dictionary<int, int> consumedItemCounts) {
			consumedItemCounts = new();

			foreach (Item item in context.recipe.requiredItem) {
				int count = isAvailable_ItemCountsDictionary.TryGetValue(item.type, out int c) ? c : 0;
				int consume = item.stack * batches;

				if (count >= consume) {
					isAvailable_ItemCountsDictionary[item.type] = count - consume;
					consumedItemCounts.AddOrSumCount(item.type, consume);
				} else {
					// Item would be wholly consumed
					if (count > 0) {
						consume -= count;
						isAvailable_ItemCountsDictionary.Remove(item.type);
						consumedItemCounts.AddOrSumCount(item.type, count);
					}

					// Check for recipe groups
					foreach (int groupID in context.recipe.acceptedGroups) {
						RecipeGroup group = RecipeGroup.recipeGroups[groupID];
						if (group.ContainsItem(item.type)) {
							// Check each valid item in the group
							foreach (int groupItem in group.ValidItems) {
								count = isAvailable_ItemCountsDictionary.TryGetValue(groupItem, out c) ? c : 0;

								if (count >= consume) {
									// Nothing left to consume
									isAvailable_ItemCountsDictionary[groupItem] = count - consume;
									consumedItemCounts.AddOrSumCount(groupItem, consume);
									goto afterGroupCheck;
								} else if (count > 0) {
									// Item would be wholly consumed
									consume -= count;
									isAvailable_ItemCountsDictionary.Remove(groupItem);
									consumedItemCounts.AddOrSumCount(groupItem, count);
								}
							}
						}
					}

					afterGroupCheck: ;
				}
			}
		}

		private static bool IsAvailable_CheckRecipe(Recipe recipe) {
			if (recipe is null)
				return false;

			if (recipe.requiredTile.Any(tile => !adjTiles[tile]))
				return false;

			foreach (Item ingredient in recipe.requiredItem)
			{
				if (ingredient.stack - IsAvailable_GetItemCount(recipe, ingredient.type) > 0)
					return false;
			}

			if (currentlyThreading)
				return StorageGUI.activeThread.state is ThreadState state && state.recipeConditionsMetSnapshot[recipe.RecipeIndex];

			bool retValue = true;

			ExecuteInCraftingGuiEnvironment(() => {
				if (!RecipeLoader.RecipeAvailable(recipe))
					retValue = false;
			});

			return retValue;
		}

		private static Dictionary<int, int> isAvailable_ItemCountsDictionary;

		private static int IsAvailable_GetItemCount(Recipe recipe, int type) {
			ClampedArithmetic count = 0;
			bool useRecipeGroup = false;
			foreach (var (item, quantity) in isAvailable_ItemCountsDictionary) {
				if (RecipeGroupMatch(recipe, item, type)) {
					count += quantity;
					useRecipeGroup = true;
				}
			}

			if (!useRecipeGroup && isAvailable_ItemCountsDictionary.TryGetValue(type, out int amount))
				count += amount;

			return count;
		}

		public class PlayerZoneCache {
			public readonly bool[] origAdjTile;
			public readonly bool oldAdjWater;
			public readonly bool oldAdjLava;
			public readonly bool oldAdjHoney;
			public readonly bool oldAlchemyTable;
			public readonly bool oldSnow;
			public readonly bool oldGraveyard;

			private PlayerZoneCache() {
				Player player = Main.LocalPlayer;
				origAdjTile = player.adjTile.ToArray();
				oldAdjWater = player.adjWater;
				oldAdjLava = player.adjLava;
				oldAdjHoney = player.adjHoney;
				oldAlchemyTable = player.alchemyTable;
				oldSnow = player.ZoneSnow;
				oldGraveyard = player.ZoneGraveyard;
			}

			private static PlayerZoneCache cache;

			public static void Cache() {
				if (cache is not null)
					return;

				cache = new PlayerZoneCache();
			}

			public static void FreeCache(bool destroy) {
				if (cache is not PlayerZoneCache c)
					return;

				if (destroy)
					cache = null;

				Player player = Main.LocalPlayer;

				player.adjTile = c.origAdjTile;
				player.adjWater = c.oldAdjWater;
				player.adjLava = c.oldAdjLava;
				player.adjHoney = c.oldAdjHoney;
				player.alchemyTable = c.oldAlchemyTable;
				player.ZoneSnow = c.oldSnow;
				player.ZoneGraveyard = c.oldGraveyard;
			}
		}

		internal static void ExecuteInCraftingGuiEnvironment(Action action)
		{
			ArgumentNullException.ThrowIfNull(action);

			PlayerZoneCache.Cache();

			Player player = Main.LocalPlayer;

			try
			{
				player.adjTile = adjTiles;
				player.adjWater = adjWater;
				player.adjLava = adjLava;
				player.adjHoney = adjHoney;
				player.alchemyTable = alchemyTable;
				player.ZoneSnow = zoneSnow;
				player.ZoneGraveyard = graveyard;

				action();
			} finally {
				PlayerZoneCache.FreeCache(false);
			}
		}

		private static List<ItemInfo> storageItemInfo;

		internal static bool PassesBlock(Recipe recipe)
		{
			if (recipe is null || storageItemInfo is null)
				return false;

			NetHelper.Report(true, "Checking if recipe passes \"blocked ingredients\" check...");

			if (GetCraftingTree(recipe) is OrderedRecipeTree craftingTree) {
				isAvailable_ItemCountsDictionary = itemCounts;
				craftingTree.TrimBranches(IsAvailable_GetItemCount);
				isAvailable_ItemCountsDictionary = null;

				// Data list will need to be modified.  Cache the old value
				List<ItemInfo> oldInfo = new List<ItemInfo>(storageItemInfo);

				Stack<OrderedRecipeContext> recipeStack = craftingTree.GetProcessingOrder();

				foreach (OrderedRecipeContext context in recipeStack) {
					// Trimmed branch?  Ignore
					if (context.amountToCraft <= 0)
						continue;

					Recipe contextRecipe = context.recipe;

					if (!PassesBlock_CheckRecipe(contextRecipe, consumeStoredItems: true)) {
						storageItemInfo = oldInfo;
						NetHelper.Report(true, "Recipe failed the ingredients check");
						return false;
					}

					// Add the result items to the list
					Item resultItem = contextRecipe.createItem.Clone();
					resultItem.stack = context.amountToCraft;
					resultItem.Prefix(-1);

					int resultStack = resultItem.stack;

					for (int i = 0; i < storageItemInfo.Count; i++) {
						ItemInfo info = storageItemInfo[i];

						if (info.type == resultItem.type && info.prefix == resultItem.prefix && info.stack < resultItem.maxStack) {
							// Consume from the list
							if (info.stack + resultStack <= resultItem.maxStack) {
								info = new ItemInfo(info.type, resultStack + info.stack, info.prefix);
								resultStack = 0;
							} else {
								info = new ItemInfo(info.type, resultItem.maxStack, info.prefix);
								resultStack -= resultItem.maxStack - info.stack;
							}

							storageItemInfo[i] = info;
						}

						if (resultStack <= 0)
							break;
					}

					if (resultStack > 0)
						storageItemInfo.Add(new ItemInfo(resultItem));
				}

				storageItemInfo = oldInfo;

				NetHelper.Report(true, "Recipe passed the ingredients check");
				return true;
			} else {
				bool success = PassesBlock_CheckRecipe(recipe);
				NetHelper.Report(true, $"Recipe {(success ? "passed" : "failed")} the ingredients check");
				return success;
			}
		}

		private static bool PassesBlock_CheckRecipe(Recipe recipe, bool consumeStoredItems = false) {
			foreach (Item ingredient in recipe.requiredItem) {
				int stack = ingredient.stack;
				bool useRecipeGroup = false;

				int storageIndex = -1;
				List<int> updatedStorageStacks = new();
				foreach (ItemInfo item in storageItemInfo) {
					storageIndex++;
					updatedStorageStacks.Add(item.stack);

					if (stack <= 0)
						continue;

					if (!blockStorageItems.Contains(new ItemData(item)) && RecipeGroupMatch(recipe, item.type, ingredient.type)) {
						if (consumeStoredItems) {
							if (item.stack >= stack)
								updatedStorageStacks[storageIndex] -= stack;
							else
								updatedStorageStacks[storageIndex] = 0;
						}

						stack -= item.stack;
						useRecipeGroup = true;
					}
				}

				if (!useRecipeGroup) {
					storageIndex = -1;
					foreach (ItemInfo item in storageItemInfo) {
						storageIndex++;

						if (!blockStorageItems.Contains(new ItemData(item)) && item.type == ingredient.type) {
							if (consumeStoredItems) {
								if (item.stack >= stack)
									updatedStorageStacks[storageIndex] -= stack;
								else
									updatedStorageStacks[storageIndex] = 0;
							}

							stack -= item.stack;

							if (stack <= 0)
								break;
						}
					}
				}

				// Update the list with the new stacks
				if (consumeStoredItems) {
					Stack<int> toRemove = new();

					for (int i = 0; i < updatedStorageStacks.Count; i++) {
						int updatedStack = updatedStorageStacks[i];
						if (updatedStack <= 0) {
							toRemove.Push(i);
							continue;
						}

						ItemInfo info = storageItemInfo[i];
						storageItemInfo[i] = new ItemInfo(info.type, updatedStack, info.prefix);
					}

					foreach (int index in toRemove)
						storageItemInfo.RemoveAt(index);
				}

				if (stack > 0)
					return false;
			}

			return true;
		}

		private static void RefreshStorageItems()
		{
			NetHelper.Report(true, "Updating stored ingredients collection and result item...");

			storageItems.Clear();
			storageItemInfo = new();
			storageItemsFromModules.Clear();
			result = null;
			if (selectedRecipe is null) {
				NetHelper.Report(true, "Failed.  No recipe is selected.");
				return;
			}

			int index = 0;
			bool hasItemFromStorage = false;
			if (!MagicStorageConfig.IsRecursionEnabled || !selectedRecipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				NetHelper.Report(false, "Recursion was disabled or recipe did not have a recursive recipe");

				foreach (List<Item> itemsFromSource in sourceItems) {
					CheckStorageItemsForRecipe(selectedRecipe, itemsFromSource, null, checkResultItem: true, index, ref hasItemFromStorage);
					index++;
				}
			} else {
				NetHelper.Report(false, "Recipe had a recursive recipe, processing recursion tree...");

				// Check each recipe in the tree
				List<Recipe> recipes = recursiveRecipe.GetAllRecipes().ToList();

				foreach (List<Item> itemsFromSource in sourceItems) {
					bool[] wasItemAdded = new bool[itemsFromSource.Count];
					bool checkedHighestRecipe = false;

					foreach (Recipe recipe in recipes) {
						// Only allow the "final recipe" to affect the result item
						CheckStorageItemsForRecipe(recipe, itemsFromSource, wasItemAdded, checkResultItem: !checkedHighestRecipe, index, ref hasItemFromStorage);
						checkedHighestRecipe = true;
					}

					index++;
				}
			}

			var resultItemList = CompactItemListWithModuleData(storageItems, storageItemsFromModules, out var moduleItemsList);
			if (resultItemList.Count != storageItems.Count) {
				//Update the lists since items were compacted
				storageItems.Clear();
				storageItems.AddRange(resultItemList);
				storageItemInfo.Clear();
				storageItemInfo.AddRange(storageItems.Select(static i => new ItemInfo(i)));
				storageItemsFromModules.Clear();
				storageItemsFromModules.AddRange(moduleItemsList);
			}

			result ??= new Item(selectedRecipe.createItem.type, 0);

			NetHelper.Report(true, $"Success! Found {storageItems.Count} items and {(result.IsAir ? "no result items" : "a result item")}");
		}

		private static void CheckStorageItemsForRecipe(Recipe recipe, List<Item> itemsFromSource, bool[] wasItemAdded, bool checkResultItem, int index, ref bool hasItemFromStorage) {
			int addedIndex = 0;

			foreach (Item item in itemsFromSource) {
				if (wasItemAdded?[addedIndex] is false) {
					foreach (Item reqItem in recipe.requiredItem) {
						if (item.type == reqItem.type || RecipeGroupMatch(recipe, item.type, reqItem.type)) {
							//Module items must refer to the original item instances
							Item clone = index >= numItemsWithoutSimulators ? item : item.Clone();
							storageItems.Add(clone);
							storageItemInfo.Add(new(clone));
							storageItemsFromModules.Add(index >= numItemsWithoutSimulators);

							wasItemAdded[addedIndex] = true;
						}
					}
				}

				addedIndex++;

				if (checkResultItem && item.type == recipe.createItem.type) {
					Item source = itemsFromSource[0];

					if (index < numItemsWithoutSimulators) {
						result = source;
						hasItemFromStorage = true;
					} else if (!hasItemFromStorage)
						result = source;
				}
			}
		}

		public static bool RecipeGroupMatch(Recipe recipe, int inventoryType, int requiredType)
		{
			foreach (int num in recipe.acceptedGroups)
			{
				RecipeGroup recipeGroup = RecipeGroup.recipeGroups[num];
				if (recipeGroup.ContainsItem(inventoryType) && recipeGroup.ContainsItem(requiredType))
					return true;
			}

			return false;
		}

		internal static void SetSelectedRecipe(Recipe recipe)
		{
			ArgumentNullException.ThrowIfNull(recipe);

			NetHelper.Report(true, "Reassigning current recipe...");

			selectedRecipe = recipe;
			RefreshStorageItems();
			blockStorageItems.Clear();

			SetNextDefaultRecipeCollectionToRefresh(Array.Empty<Recipe>());

			NetHelper.Report(true, "Successfully reassigned current recipe!");
		}

		internal static void SlotFocusLogic()
		{
			if (result == null || result.IsAir || !Main.mouseItem.IsAir && (!ItemCombining.CanCombineItems(Main.mouseItem, result) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
			{
				ResetSlotFocus();
			}
			else
			{
				if (rightClickTimer <= 0)
				{
					rightClickTimer = maxRightClickTimer;
					maxRightClickTimer = maxRightClickTimer * 3 / 4;
					if (maxRightClickTimer <= 0)
						maxRightClickTimer = 1;
					Item toWithdraw = result.Clone();
					toWithdraw.stack = 1;
					Item withdrawn = DoWithdrawResult(toWithdraw);
					if (Main.mouseItem.IsAir)
						Main.mouseItem = withdrawn;
					else {
						Utility.CallOnStackHooks(Main.mouseItem, withdrawn, withdrawn.stack);

						Main.mouseItem.stack += withdrawn.stack;
					}

					SoundEngine.PlaySound(SoundID.MenuTick);
					
					StorageGUI.SetRefresh();
				}

				rightClickTimer--;
			}
		}

		internal static void ResetSlotFocus()
		{
			slotFocus = false;
			rightClickTimer = 0;
			maxRightClickTimer = StartMaxRightClickTimer;
		}

		private static List<Item> CompactItemList(List<Item> items) {
			List<Item> compacted = new();

			for (int i = 0; i < items.Count; i++) {
				Item item = items[i];

				if (item.IsAir)
					continue;

				bool fullyCompacted = false;
				for (int j = 0; j < compacted.Count; j++) {
					Item existing = compacted[j];

					if (ItemCombining.CanCombineItems(item, existing)) {
						if (existing.stack + item.stack <= existing.maxStack) {
							Utility.CallOnStackHooks(existing, item, item.stack);

							existing.stack += item.stack;
							item.stack = 0;
							fullyCompacted = true;
						} else {
							int diff = existing.maxStack - existing.stack;

							Utility.CallOnStackHooks(existing, item, diff);

							existing.stack = existing.maxStack;
							item.stack -= diff;
						}

						break;
					}
				}

				if (item.IsAir)
					continue;

				if (!fullyCompacted)
					compacted.Add(item);
			}

			return compacted;
		}

		private static List<Item> CompactItemListWithModuleData(List<Item> items, List<bool> moduleItems, out List<bool> moduleItemsResult) {
			List<Item> compacted = new();
			List<int> compactedSource = new();

			for (int i = 0; i < items.Count; i++) {
				Item item = items[i];

				if (item.IsAir)
					continue;

				bool fullyCompacted = false;
				for (int j = 0; j < compacted.Count; j++) {
					Item existing = compacted[j];

					if (ItemCombining.CanCombineItems(item, existing) && moduleItems[i] == moduleItems[compactedSource[j]] && !moduleItems[i]) {
						if (existing.stack + item.stack <= existing.maxStack) {
							existing.stack += item.stack;
							item.stack = 0;
							fullyCompacted = true;
						} else {
							int diff = existing.maxStack - existing.stack;

							Utility.CallOnStackHooks(existing, item, diff);

							existing.stack = existing.maxStack;
							item.stack -= diff;
						}

						break;
					}
				}

				if (item.IsAir)
					continue;

				if (!fullyCompacted) {
					compacted.Add(item);
					compactedSource.Add(i);
				}
			}

			moduleItemsResult = compactedSource.Select(m => moduleItems[m]).ToList();

			return compacted;
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from a Crafting Access
		/// </summary>
		/// <param name="craftingAccess">The tile entity for the Crafting Access to craft items from</param>
		/// <param name="toCraft">How many items should be crafted</param>
		public static void Craft(TECraftingAccess craftingAccess, int toCraft) {
			if (craftingAccess is null)
				return;

			StoragePlayer.StorageHeartAccessWrapper wrapper = new(craftingAccess);

			//OpenStorage() handles setting the CraftingGUI to use the new storage and Dispose()/CloseStorage() handles reverting it back
			if (wrapper.Valid) {
				using (wrapper.OpenStorage())
					Craft(toCraft);
			}
		}

		private class CraftingContext {
			public List<Item> sourceItems, availableItems, toWithdraw, results;

			public List<bool> fromModule;

			public EnvironmentSandbox sandbox;

			public List<Item> consumedItemsFromModules;

			public IEnumerable<EnvironmentModule> modules;

			public int toCraft;

			public bool simulation;

			public IEnumerable<Item> ConsumedItems => toWithdraw.Concat(consumedItemsFromModules);
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from the currently assigned Crafting Access.
		/// </summary>
		/// <param name="toCraft">How many items should be crafted</param>
		public static void Craft(int toCraft) {
			NetHelper.Report(true, $"Attempting to craft {toCraft} items...");

			// Additional safeguard against absurdly high craft targets
			int origCraftRequest = toCraft;
			toCraft = Math.Min(toCraft, AmountCraftableForCurrentRecipe());

			if (toCraft != origCraftRequest)
				NetHelper.Report(false, $"Craft amount reduced to {toCraft}");

			if (toCraft <= 0)
				return;  // Bail

			CraftingContext context;
			if (MagicStorageConfig.IsRecursionEnabled) {
				// Recursive crafting uses special logic which can't just be injected into the previous logic
				context = Craft_WithRecursion(toCraft);

				if (context is null)
					return;  // Bail
			} else {
				context = InitCraftingContext(toCraft);

				int target = toCraft;

				ExecuteInCraftingGuiEnvironment(() => Craft_DoStandardCraft(context));

				NetHelper.Report(true, $"Crafted {target - context.toCraft} items");

				if (target == context.toCraft) {
					//Could not craft anything, bail
					return;
				}
			}

			NetHelper.Report(true, "Compacting results list...");

			context.toWithdraw = CompactItemList(context.toWithdraw);
			
			context.results = CompactItemList(context.results);

			if (Main.netMode == NetmodeID.SinglePlayer) {
				NetHelper.Report(true, "Spawning excess results on player...");

				foreach (Item item in HandleCraftWithdrawAndDeposit(GetHeart(), context.toWithdraw, context.results))
					Main.LocalPlayer.QuickSpawnItem(new EntitySource_TileEntity(GetHeart()), item, item.stack);

				StorageGUI.SetRefresh();
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.Report(true, "Sending craft results to server...");

				NetHelper.SendCraftRequest(GetHeart().Position, context.toWithdraw, context.results);
			}
		}

		private static void Craft_DoStandardCraft(CraftingContext context) {
			//Do lazy crafting first (batch loads of ingredients into one "craft"), then do normal crafting
			if (!AttemptLazyBatchCraft(context)) {
				NetHelper.Report(false, "Batch craft operation failed.  Attempting repeated crafting of a single result.");

				AttemptCraft(AttemptSingleCraft, context);
			}
		}

		private static CraftingContext InitCraftingContext(int toCraft) {
			var sourceItems = storageItems.Where(item => !blockStorageItems.Contains(new ItemData(item))).ToList();
			var availableItems = sourceItems.Select(item => item.Clone()).ToList();
			var fromModule = storageItemsFromModules.Where((_, n) => !blockStorageItems.Contains(new ItemData(storageItems[n]))).ToList();
			List<Item> toWithdraw = new(), results = new();

			TEStorageHeart heart = GetHeart();

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			return new CraftingContext() {
				sourceItems = sourceItems,
				availableItems = availableItems,
				toWithdraw = toWithdraw,
				results = results,
				sandbox = sandbox,
				consumedItemsFromModules = new(),
				fromModule = fromModule,
				modules = heart.GetModules(),
				toCraft = toCraft
			};
		}

		private static CraftingContext Craft_WithRecursion(int toCraft) {
			// Unlike normal crafting, the crafting tree has to be respected
			// This means that simple IsAvailable and AmountCraftable checks would just slow it down
			// Hence, the logic here will just assume that it's craftable and just ignore branches in the recursion tree that aren't available or are already satisfied
			if (!selectedRecipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe))
				throw new InvalidOperationException("Recipe object did not have a RecursiveRecipe object assigned to it");

			if (toCraft <= 0)
				return null;  // Bail

			CraftingContext context = InitCraftingContext(toCraft);

			NetHelper.Report(true, "Attempting recurrent crafting...");

			// Local capturing
			var ctx = context;
			
			// With the branches that are left, go from the bottom up and attempt to craft them
			ExecuteInCraftingGuiEnvironment(() => {
				NetHelper.Report(true, "Retrieving materials list...");

				List<RequiredMaterialInfo> materials;
				List<ItemInfo> excess;
				using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI))
					materials = recursiveRecipe.GetRequiredMaterials(toCraft, out excess);

				NetHelper.Report(true, "Attempting crafting...");

				// Immediately add the excess to the context's results
				ctx.results.AddRange(excess.Where(static i => i.stack > 0).Select(static i => new Item(i.type, i.stack, i.prefix)));

				// At this point, the amount to craft has already been clamped by the max amount possible
				// Hence, just consume the items
				List<Item> consumedItems = new();

				ctx.simulation = true;

				foreach (var m in materials) {
					var material = m;

					List<Item> origWithdraw = new(ctx.toWithdraw);
					List<Item> origResults = new(ctx.results);

					foreach (int type in material.GetValidItems()) {
						Item item = new Item(type, material.stack);

						if (!CanConsumeItem(ctx, item, origWithdraw, origResults, out bool wasAvailable, out int stackConsumed)) {
							if (wasAvailable)
								NetHelper.Report(false, $"Skipping consumption of item \"{Lang.GetItemNameValue(item.type)}\"");
							else {
								// Did not have enough items
								return;
							}
						} else {
							// Consume the item
							material = material.SetStack(stackConsumed);
							item.stack = stackConsumed;
							consumedItems.Add(item);

							if (material.stack <= 0)
								break;
						}
					}
				}

				ctx.simulation = false;

				// Actually consume the items
				foreach (Item item in consumedItems) {
					int stack = item.stack;

					if (!AttemptToConsumeItem(ctx, ctx.results, item.type, ref stack, addToWithdraw: false)) {
						if (!AttemptToConsumeItem(ctx, ctx.availableItems, item.type, ref stack, addToWithdraw: true))
							ConsumeItemFromSource(ctx, item.type, item.stack);
					}
				}

				// Last item in the "excess results" list is always the main recipe's item
				NetHelper.Report(true, $"Success! Crafted {excess[^1].stack} items and {excess.Count - 1} extra item types");
			});

			// Sanity check
			selectedRecipe = recursiveRecipe.original;

			return context;
		}

		private static void AttemptCraft(Func<CraftingContext, bool> func, CraftingContext context) {
			while (context.toCraft > 0) {
				if (!func(context))
					break;  // Could not craft any more items

				Item resultItem = selectedRecipe.createItem.Clone();
				context.toCraft -= resultItem.stack;

				resultItem.Prefix(-1);
				context.results.Add(resultItem);

				CatchDroppedItems = true;
				DroppedItems.Clear();

				var consumed = context.ConsumedItems.ToList();

				RecipeLoader.OnCraft(resultItem, selectedRecipe, consumed, new Item());

				foreach (EnvironmentModule module in context.modules)
					module.OnConsumeItemsForRecipe(context.sandbox, selectedRecipe, consumed);

				context.consumedItemsFromModules.Clear();

				CatchDroppedItems = false;

				context.results.AddRange(DroppedItems);
			}
		}

		private static bool AttemptLazyBatchCraft(CraftingContext context) {
			NetHelper.Report(false, "Attempting batch craft operation...");

			List<Item> origResults = new(context.results);
			List<Item> origWithdraw = new(context.toWithdraw);

			//Try to batch as many "crafts" into one craft as possible
			int crafts = (int)Math.Ceiling(context.toCraft / (float)selectedRecipe.createItem.stack);

			//Skip item consumption code for recipes that have no ingredients
			if (selectedRecipe.requiredItem.Count == 0)
				goto SkipItemConsumption;

			context.simulation = true;

			List<Item> batch = new(selectedRecipe.requiredItem.Count);

			//Reduce the number of batch crafts until this recipe can be completely batched for the number of crafts
			while (crafts > 0) {
				foreach (Item reqItem in selectedRecipe.requiredItem) {
					Item clone = reqItem.Clone();
					clone.stack *= crafts;

					if (!CanConsumeItem(context, clone, origWithdraw, origResults, out bool wasAvailable, out int stackConsumed)) {
						if (wasAvailable)
							NetHelper.Report(false, $"Skipping consumption of item \"{Lang.GetItemNameValue(reqItem.type)}\". (Batching {crafts} crafts)");
						else {
							// Did not have enough items
							crafts--;
							batch.Clear();
						}
					} else {
						//Consume the item
						clone.stack = stackConsumed;
						batch.Add(clone);
					}
				}

				if (batch.Count > 0) {
					//Successfully batched items for the craft
					break;
				}
			}

			// Remove any empty items since they wouldn't do anything anyway
			batch.RemoveAll(i => i.stack <= 0);

			context.simulation = false;

			if (crafts <= 0) {
				//Craft batching failed
				return false;
			}

			//Consume the batched items
			foreach (Item item in batch) {
				int stack = item.stack;

				if (!AttemptToConsumeItem(context, context.availableItems, item.type, ref stack, addToWithdraw: true))
					ConsumeItemFromSource(context, item.type, item.stack);
			}

			SkipItemConsumption:

			//Create the resulting items
			for (int i = 0; i < crafts; i++) {
				Item resultItem = selectedRecipe.createItem.Clone();
				context.toCraft -= resultItem.stack;

				resultItem.Prefix(-1);
				context.results.Add(resultItem);

				CatchDroppedItems = true;
				DroppedItems.Clear();

				var consumed = context.ConsumedItems.ToList();

				RecipeLoader.OnCraft(resultItem, selectedRecipe, consumed, new Item());

				foreach (EnvironmentModule module in context.modules)
					module.OnConsumeItemsForRecipe(context.sandbox, selectedRecipe, consumed);

				CatchDroppedItems = false;

				context.results.AddRange(DroppedItems);
			}

			NetHelper.Report(false, $"Batch craft operation succeeded ({crafts} crafts batched)");

			return true;
		}

		private static bool AttemptSingleCraft(CraftingContext context) {
			List<Item> origResults = new(context.results);
			List<Item> origWithdraw = new(context.toWithdraw);

			NetHelper.Report(false, "Attempting one craft operation...");

			List<int> stacksConsumed = new();

			foreach (Item reqItem in selectedRecipe.requiredItem) {
				if (!CanConsumeItem(context, reqItem, origWithdraw, origResults, out bool wasAvailable, out int stackConsumed)) {
					if (wasAvailable)
						NetHelper.Report(false, $"Skipping consumption of item \"{Lang.GetItemNameValue(reqItem.type)}\".");
					else {
						NetHelper.Report(false, $"Required item \"{Lang.GetItemNameValue(reqItem.type)}\" was not available.");
						return false;  // Did not have enough items
					}
				} else
					NetHelper.Report(false, $"Required item \"{Lang.GetItemNameValue(reqItem.type)}\" was available.");

				stacksConsumed.Add(stackConsumed);
			}

			//Consume the source items as well since the craft was successful
			int consumeStackIndex = 0;
			foreach (Item reqItem in selectedRecipe.requiredItem) {
				ConsumeItemFromSource(context, reqItem.type, stacksConsumed[consumeStackIndex]);

				consumeStackIndex++;
			}

			NetHelper.Report(false, "Craft operation succeeded");

			return true;
		}

		private static bool CanConsumeItem(CraftingContext context, Item reqItem, List<Item> origWithdraw, List<Item> origResults, out bool wasAvailable, out int stackConsumed) {
			wasAvailable = true;

			stackConsumed = reqItem.stack;

			RecipeLoader.ConsumeItem(selectedRecipe, reqItem.type, ref stackConsumed);

			foreach (EnvironmentModule module in context.modules)
				module.ConsumeItemForRecipe(context.sandbox, selectedRecipe, reqItem.type, ref stackConsumed);

			if (stackConsumed <= 0)
				return false;

			int stack = stackConsumed;
			bool consumeSucceeded = AttemptToConsumeItem(context, context.availableItems, reqItem.type, ref stack, addToWithdraw: true);

			if (!consumeSucceeded) {
				stack = stackConsumed;
				consumeSucceeded = AttemptToConsumeItem(context, context.results, reqItem.type, ref stack, addToWithdraw: false);
			}

			if (stack > 0 || !consumeSucceeded) {
				context.results.Clear();
				context.results.AddRange(origResults);

				context.toWithdraw.Clear();
				context.toWithdraw.AddRange(origWithdraw);

				wasAvailable = false;
				return false;
			}

			return true;
		}

		private static bool AttemptToConsumeItem(CraftingContext context, List<Item> list, int reqType, ref int stack, bool addToWithdraw) {
			int listIndex = 0;
			foreach (Item tryItem in !context.simulation ? list : list.Select(i => new Item(i.type, i.stack))) {
				if (reqType == tryItem.type || RecipeGroupMatch(selectedRecipe, tryItem.type, reqType)) {
					//Don't attempt to withdraw if the item is from a module, since it doesn't exist in the storage system anyway
					bool canWithdraw = context.simulation || (addToWithdraw && !context.fromModule[listIndex]);

					int stackToConsume;

					if (tryItem.stack > stack) {
						stackToConsume = stack;
						stack = 0;
					} else {
						stackToConsume = tryItem.stack;
						stack -= tryItem.stack;
					}

					if (!context.simulation)
						OnConsumeItemForRecipe_Obsolete(context, tryItem, stackToConsume);

					if (addToWithdraw && !context.simulation && !context.fromModule[listIndex]) {
						Item temp = tryItem.Clone();
						temp.stack = stackToConsume;

						context.toWithdraw.Add(temp);
					}

					tryItem.stack -= stackToConsume;

					if (tryItem.stack <= 0)
						tryItem.type = ItemID.None;

					if (stack <= 0)
						break;
				}

				listIndex++;
			}

			return stack <= 0;
		}

		private static void ConsumeItemFromSource(CraftingContext context, int reqType, int stack) {
			int index = 0;
			foreach (Item tryItem in context.sourceItems) {
				if (reqType == tryItem.type || RecipeGroupMatch(selectedRecipe, tryItem.type, reqType)) {
					int stackToConsume;

					if (tryItem.stack > stack) {
						stackToConsume = stack;
						stack = 0;
					} else {
						stackToConsume = tryItem.stack;
						stack -= tryItem.stack;
					}

					OnConsumeItemForRecipe_Obsolete(context, tryItem, stackToConsume);

					Item consumed = tryItem.Clone();
					consumed.stack = stackToConsume;

					context.consumedItemsFromModules.Add(consumed);

					//Items should only be consumed if they're from a module, since withdrawing wouldn't grab the item anyway
					if (context.fromModule[index]) {
						tryItem.stack -= stackToConsume;

						if (tryItem.stack <= 0)
							tryItem.type = ItemID.None;
					}

					if (stack <= 0)
						break;
				}

				index++;
			}
		}

		[Obsolete]
		private static void OnConsumeItemForRecipe_Obsolete(CraftingContext context, Item tryItem, int stackToConsume) {
			foreach (var module in context.modules)
				module.OnConsumeItemForRecipe(context.sandbox, tryItem, stackToConsume);
		}

		internal static List<Item> HandleCraftWithdrawAndDeposit(TEStorageHeart heart, List<Item> toWithdraw, List<Item> results)
		{
			var items = new List<Item>();
			foreach (Item tryWithdraw in toWithdraw)
			{
				Item withdrawn = heart.TryWithdraw(tryWithdraw, false);
				if (!withdrawn.IsAir)
					items.Add(withdrawn);
				if (withdrawn.stack < tryWithdraw.stack)
				{
					for (int k = 0; k < items.Count; k++)
					{
						heart.DepositItem(items[k]);
						if (items[k].IsAir)
						{
							items.RemoveAt(k);
							k--;
						}
					}

					return items;
				}
			}

			items.Clear();
			foreach (Item result in results)
			{
				heart.DepositItem(result);
				if (!result.IsAir)
					items.Add(result);
			}

			return items;
		}

		internal static bool TryDepositResult(Item item)
		{
			int oldStack = item.stack;
			int oldType = item.type;
			TEStorageHeart heart = GetHeart();
			heart.TryDeposit(item);

			if (oldStack != item.stack) {
				SetNextDefaultRecipeCollectionToRefresh(oldType);

				return true;
			}

			return false;
		}

		internal static Item DoWithdrawResult(Item item, bool toInventory = false)
		{
			TEStorageHeart heart = GetHeart();
			Item withdrawn = heart.TryWithdraw(item, false, toInventory);

			if (withdrawn.IsAir && items.Count != numItemsWithoutSimulators) {
				//Heart did not contain the item; try to withdraw from the module items
				List<Item> moduleItems = items.GetRange(numItemsWithoutSimulators, items.Count - numItemsWithoutSimulators);

				TEStorageUnit.WithdrawFromItemCollection(moduleItems, item, out withdrawn,
					onItemRemoved: k => {
						int index = k + numItemsWithoutSimulators;

						foreach (var item in sourceItems[index])
							item.TurnToAir();

						items.RemoveAt(index);
						sourceItems.RemoveAt(index);
					},
					onItemStackReduced: (k, stack) => {
						int index = k + numItemsWithoutSimulators;

						int itemsRemoved = 0;
						foreach (var item in Enumerable.Reverse(sourceItems[index])) {
							if (item.stack > stack) {
								item.stack -= stack;
								break;
							} else {
								stack -= item.stack;
								item.TurnToAir();

								itemsRemoved++;

								if (stack <= 0)
									break;
							}
						}

						if (itemsRemoved > 0)
							sourceItems[index].RemoveRange(sourceItems[index].Count - itemsRemoved, itemsRemoved);
					});
			}

			if (!withdrawn.IsAir) {
				StorageGUI.SetRefresh();
				SetNextDefaultRecipeCollectionToRefresh(MagicCache.RecipesUsingItemType.TryGetValue(withdrawn.type, out var result) ? result.Value : null);
			}

			return withdrawn;
		}
	}
}
