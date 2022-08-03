using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Items;
using MagicStorage.Sorting;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	public static class CraftingGUI
	{
		public const int RecipeButtonsAvailableChoice = 0;
		public const int RecipeButtonsBlacklistChoice = 3;
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

		private static readonly List<Item> items = new();

		private static readonly Dictionary<int, int> itemCounts = new();
		internal static List<Recipe> recipes = new();
		internal static List<bool> recipeAvailable = new();
		internal static Recipe selectedRecipe;

		internal static bool slotFocus;

		internal static readonly List<Item> storageItems = new();
		internal static readonly List<bool> storageItemsFromModules = new();
		internal static readonly List<ItemData> blockStorageItems = new();

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
			if (selectedRecipe == null)
				return new Item();

			// TODO: Can we simply return `selectedRecipe.createItem`
			Item item = selectedRecipe.createItem;
			if (item.IsAir)
				item = new Item(item.type, 0);

			return item;
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

			foreach (RecipeGroup rec in selectedRecipe.acceptedGroups.Select(index => RecipeGroup.recipeGroups[index]))
				if (rec.ValidItems.Contains(item.type))
					foreach (int type in rec.ValidItems)
						totalGroupStack += storageItems.Where(i => i.type == type).Sum(i => i.stack);

			if (!item.IsAir)
			{
				if (storageItem.IsAir && totalGroupStack == 0)
					context = 3; // Unavailable - Red // ItemSlot.Context.ChestItem
				else if (storageItem.stack < item.stack && totalGroupStack < item.stack)
					context = 4; // Partially in stock - Pinkish // ItemSlot.Context.BankItem
				// context == 0 - Available - Default Blue
				if (context != 0)
				{
					bool craftable = MagicCache.ResultToRecipe.TryGetValue(item.type, out var r) && r.Any(recipe => AmountCraftable(recipe) > 0);
					if (craftable)
						context = 6; // Craftable - Light green // ItemSlot.Context.TrashItem
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

		// Calculates how many times a recipe can be crafted using available items
		// TODO is this correct?
		internal static int AmountCraftable(Recipe recipe)
		{
			if (!IsAvailable(recipe))
				return 0;
			int maxCraftable = int.MaxValue;

			if (RecursiveCraftIntegration.Enabled)
				recipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);

			int GetAmountCraftable(Item requiredItem)
			{
				int total = 0;
				foreach (Item inventoryItem in items)
					if (inventoryItem.type == requiredItem.type || RecipeGroupMatch(recipe, inventoryItem.type, requiredItem.type))
						total += inventoryItem.stack;

				int craftable = total / requiredItem.stack;
				return craftable;
			}

			maxCraftable = recipe.requiredItem.Select(GetAmountCraftable).Prepend(maxCraftable).Min();

			return maxCraftable;
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
				if (RecursiveCraftIntegration.Enabled)
				{
					RecursiveCraftIntegration.RefreshRecursiveRecipes();
					if (RecursiveCraftIntegration.HasCompoundVariant(selectedRecipe))
						SetSelectedRecipe(selectedRecipe);
				}

				IEnumerable<int> allItemTypes = new int[] { selectedRecipe.createItem.type }.Concat(selectedRecipe.requiredItem.Select(i => i.type));

				IEnumerable<Recipe> affectedRecipes = allItemTypes.SelectMany(i => MagicCache.RecipesUsingItemType.TryGetValue(i, out var recipes) ? recipes.Value : Array.Empty<Recipe>());

				//If no recipes were affected, that's fine, none of the recipes will be touched due to the array being empty
				RefreshItemsAndSpecificRecipes(affectedRecipes.ToArray());
				SoundEngine.PlaySound(SoundID.Grab);
			}

			craftTimer--;
			stillCrafting = true;
		}

		internal static void ClickAmountButton(int amount, bool offset) {
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
			else if (!IsAvailable(selectedRecipe, false) || !PassesBlock(selectedRecipe))
				craftAmountTarget = 1;
			else if (craftAmountTarget > selectedRecipe.createItem.maxStack)
				craftAmountTarget = selectedRecipe.createItem.maxStack;
		}

		internal static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		internal static TECraftingAccess GetCraftingEntity() => StoragePlayer.LocalPlayer.GetCraftingAccess();

		internal static List<Item> GetCraftingStations() => GetCraftingEntity()?.stations;

		public static void RefreshItems() => RefreshItemsAndSpecificRecipes(null);

		private static int numItemsWithoutSimulators;

		private static void RefreshItemsAndSpecificRecipes(Recipe[] toRefresh) {
			items.Clear();
			numItemsWithoutSimulators = 0;
			TEStorageHeart heart = GetHeart();
			if (heart == null) {
				StorageGUI.InvokeOnRefresh();
				StorageGUI.needRefresh = false;
				return;
			}

			NetHelper.Report(true, "CraftingGUI: RefreshItemsAndSpecificRecipes invoked");

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			IEnumerable<Item> heartItems = heart.GetStoredItems().Select(i => i.Clone());
			IEnumerable<Item> simulatorItems = heart.GetModules().SelectMany(m => m.GetAdditionalItems(sandbox) ?? Array.Empty<Item>())
				.DistinctBy(i => i, ReferenceEqualityComparer.Instance);  //Filter by distinct object references (prevents "duplicate" items from, say, 2 mods adding items from the player's inventory)

			//Keep the simulator items separate
			items.AddRange(ItemSorter.SortAndFilter(heartItems, SortingOptionLoader.Definitions.ID.Type, FilteringOptionLoader.Definitions.All.Type, ""));

			numItemsWithoutSimulators = items.Count;

			items.AddRange(ItemSorter.SortAndFilter(simulatorItems, SortingOptionLoader.Definitions.ID.Type, FilteringOptionLoader.Definitions.All.Type, ""));

			NetHelper.Report(false, "Total items: " + items.Count);
			NetHelper.Report(false, "Items from modules: " + (items.Count - numItemsWithoutSimulators));

			AnalyzeIngredients();

			RefreshStorageItems();

			try {
				NetHelper.Report(false, "Visible recipes: " + recipes.Count);
				NetHelper.Report(false, "Available recipes: " + recipeAvailable.Count(b => b));

				if (toRefresh is null)
					RefreshRecipes();  //Refresh all recipes
				else
					RefreshSpecificRecipes(toRefresh);

				NetHelper.Report(true, "Recipe refreshing finished");

				// TODO is there a better way?
				void GuttedSetSelectedRecipe(Recipe recipe, int index)
				{
					Recipe compound = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);
					if (index != -1)
						recipes[index] = compound;

					selectedRecipe = compound;
					RefreshStorageItems();
					blockStorageItems.Clear();
				}

				if (RecursiveCraftIntegration.Enabled) {
					if (selectedRecipe is not null)
					{
						// If the selected recipe is compound, replace the overridden recipe with the compound one so it shows as selected in the UI
						if (RecursiveCraftIntegration.IsCompoundRecipe(selectedRecipe))
						{
							Recipe overridden = RecursiveCraftIntegration.GetOverriddenRecipe(selectedRecipe);
							int index = recipes.IndexOf(overridden);
							GuttedSetSelectedRecipe(overridden, index);
						}
						// If the selectedRecipe(which isn't compound) is uncraftable but is in the available list, this means it's compound version is craftable
						else if (!IsAvailable(selectedRecipe, false))
						{
							int index = recipes.IndexOf(selectedRecipe);
							if (index != -1 && recipeAvailable[index])
								GuttedSetSelectedRecipe(selectedRecipe, index);
						}
					}
				}
			}  catch (Exception e) {
				Main.NewTextMultiline(e.ToString(), c: Color.White);
			}

			if (heart is not null) {
				foreach (TEEnvironmentAccess environment in heart.GetEnvironmentSimulators())
					environment.ResetPlayer(sandbox);
			}

			StorageGUI.InvokeOnRefresh();
			StorageGUI.needRefresh = false;

			NetHelper.Report(true, "CraftingGUI: RefreshItemsAndSpecificRecipes finished");
		}

		private static void RefreshRecipes()
		{
			var page = MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting");

			string searchText = page.searchBar.Text;
			int choice = page.recipeButtons.Choice;

			void DoFiltering(int sortMode, int filterMode, ItemTypeOrderedSet hiddenRecipes, ItemTypeOrderedSet favorited)
			{
				var filteredRecipes = ItemSorter.GetRecipes(sortMode, filterMode, searchText, out var sortComparer);

				// show only blacklisted recipes only if choice = 2, otherwise show all other
				if (MagicStorageConfig.RecipeBlacklistEnabled)
					filteredRecipes = filteredRecipes.Where(x => choice == RecipeButtonsBlacklistChoice == hiddenRecipes.Contains(x.createItem));

				// favorites first
				if (MagicStorageConfig.CraftingFavoritingEnabled)
					filteredRecipes = filteredRecipes.Where(x => choice != RecipeButtonsFavoritesChoice || favorited.Contains(x.createItem));
					
				filteredRecipes = filteredRecipes.OrderBy(r => favorited.Contains(r.createItem) ? 0 : 1)
					.ThenBy(r => r.createItem, sortComparer);

				recipes.Clear();
				recipeAvailable.Clear();

				if (choice == RecipeButtonsAvailableChoice)
				{
					recipes.AddRange(filteredRecipes.Where(r => IsAvailable(r)));
					recipeAvailable.AddRange(Enumerable.Repeat(true, recipes.Count));
				}
				else
				{
					recipes.AddRange(filteredRecipes);
					recipeAvailable.AddRange(recipes.AsParallel().AsOrdered().Select(r => IsAvailable(r)));
				}
			}

			NetHelper.Report(true, "Refreshing all recipes");

			if (RecursiveCraftIntegration.Enabled)
				RecursiveCraftIntegration.RefreshRecursiveRecipes();

			int sortMode = MagicUI.craftingUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.craftingUI.GetPage<FilteringPage>("Filtering").option;

			var hiddenRecipes = StoragePlayer.LocalPlayer.HiddenRecipes;
			var favorited = StoragePlayer.LocalPlayer.FavoritedRecipes;

			DoFiltering(sortMode, filterMode, hiddenRecipes, favorited);

			// now if nothing found we disable filters one by one
			if (MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting").searchBar.Text.Length > 0)
			{
				if (recipes.Count == 0 && hiddenRecipes.Count > 0)
				{
					// search hidden recipes too
					hiddenRecipes = ItemTypeOrderedSet.Empty;
					DoFiltering(sortMode, filterMode, hiddenRecipes, favorited);
				}

				/*
				if (recipes.Count == 0 && filterMode != FilterMode.All)
				{
					// any category
					filterMode = FilterMode.All;
					DoFiltering(sortMode, filterMode, hiddenRecipes, favorited);
				}
				*/
			}
		}

		private static void RefreshSpecificRecipes(Recipe[] toRefresh) {
			NetHelper.Report(true, "Refreshing " + toRefresh.Length + " recipes");

			//Assumes that the recipes are visible in the GUI
			bool needsResort = false;

			int filterMode = MagicUI.craftingUI.GetPage<FilteringPage>("Filtering").option;
			string searchText = MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting").searchBar.Text;

			var hiddenRecipes = StoragePlayer.LocalPlayer.HiddenRecipes;
			var favorited = StoragePlayer.LocalPlayer.FavoritedRecipes;

			int recipeChoice = MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting").recipeButtons.Choice;

			bool CanBeAdded(Recipe r) => Array.IndexOf(MagicCache.FilteredRecipesCache[filterMode], r) >= 0
				&& ItemSorter.FilterBySearchText(r.createItem, searchText)
				// show only blacklisted recipes only if choice = 2, otherwise show all other
				&& (!MagicStorageConfig.RecipeBlacklistEnabled || recipeChoice == RecipeButtonsBlacklistChoice == hiddenRecipes.Contains(r.createItem))
				// show only favorited items if selected
				&& (!MagicStorageConfig.CraftingFavoritingEnabled || recipeChoice != RecipeButtonsFavoritesChoice || favorited.Contains(r.createItem));

			foreach (Recipe recipe in toRefresh) {
				Recipe orig = recipe;
				Recipe check = recipe;

				if (RecursiveCraftIntegration.Enabled && RecursiveCraftIntegration.IsCompoundRecipe(check))
					check = RecursiveCraftIntegration.ApplyCompoundRecipe(check);  //Get the compound recipe

				if (check is null)
					continue;

				int index = recipes.IndexOf(orig);  //Compound recipe is not assigned to the original recipe list

				if (!IsAvailable(check)) {
					if (index >= 0) {
						if (recipeChoice == RecipeButtonsAvailableChoice) {
							//Remove the recipe
							recipes.RemoveAt(index);
							recipeAvailable.RemoveAt(index);
						} else {
							//Simply mark the recipe as unavailable
							recipeAvailable[index] = false;
						}
					}
				} else {
					if (recipeChoice == RecipeButtonsAvailableChoice) {
						if (index < 0 && CanBeAdded(orig)) {
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

			if (needsResort) {
				int sortMode = MagicUI.craftingUI.GetPage<SortingPage>("Sorting").option;
				var sortComparer = SortingOptionLoader.Get(sortMode).Sorter;

				var sorted = new List<Recipe>(recipes)
					.AsParallel()
					.AsOrdered()
					// favorites first
					.OrderBy(r => favorited.Contains(r.createItem) ? 0 : 1)
					.ThenBy(r => r.createItem, sortComparer);

				recipes.Clear();
				recipeAvailable.Clear();

				recipes.AddRange(sorted);
				recipeAvailable.AddRange(Enumerable.Repeat(true, recipes.Count));
			}
		}

		private static void AnalyzeIngredients()
		{
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

			itemCounts.Clear();
			foreach ((int type, int amount) in items.GroupBy(item => item.type, item => item.stack, (type, stacks) => (type, stacks.Sum())))
				itemCounts[type] = amount;

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
				foreach (TEEnvironmentAccess environment in heart.GetEnvironmentSimulators())
					environment.ModifyCraftingZones(sandbox, ref information);
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

		public static bool IsAvailable(Recipe recipe, bool checkCompound = true)
		{
			if (recipe is null)
				return false;

			if (RecursiveCraftIntegration.Enabled && checkCompound)
				recipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);

			if (recipe.requiredTile.Any(tile => !adjTiles[tile]))
				return false;

			foreach (Item ingredient in recipe.requiredItem)
			{
				int stack = ingredient.stack;
				bool useRecipeGroup = false;
				foreach (var (type, count) in itemCounts)
					if (RecipeGroupMatch(recipe, type, ingredient.type))
					{
						stack -= count;
						useRecipeGroup = true;
					}

				if (!useRecipeGroup && itemCounts.TryGetValue(ingredient.type, out int amount))
					stack -= amount;

				if (stack > 0)
					return false;
			}

			bool retValue = true;

			ExecuteInCraftingGuiEnvironment(() => {
				if (!RecipeLoader.RecipeAvailable(recipe))
					retValue = false;
			});

			return retValue;
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
				origAdjTile = player.adjTile;
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
			}
			finally { }
		}

		internal static bool PassesBlock(Recipe recipe)
		{
			foreach (Item ingredient in recipe.requiredItem)
			{
				int stack = ingredient.stack;
				bool useRecipeGroup = false;

				foreach (Item item in storageItems)
				{
					ItemData data = new(item);
					if (!blockStorageItems.Contains(data) && RecipeGroupMatch(recipe, item.type, ingredient.type))
					{
						stack -= item.stack;
						useRecipeGroup = true;
					}
				}

				if (!useRecipeGroup)
					foreach (Item item in storageItems)
					{
						ItemData data = new(item);
						if (!blockStorageItems.Contains(data) && item.type == ingredient.type)
							stack -= item.stack;
					}

				if (stack > 0)
					return false;
			}


			return true;
		}

		private static void RefreshStorageItems()
		{
			storageItems.Clear();
			storageItemsFromModules.Clear();
			result = null;
			if (selectedRecipe is null)
				return;

			int index = 0;
			foreach (Item item in items)
			{
				foreach (Item reqItem in selectedRecipe.requiredItem) {
					if (item.type == reqItem.type || RecipeGroupMatch(selectedRecipe, item.type, reqItem.type)) {
						storageItems.Add(item);
						storageItemsFromModules.Add(index >= numItemsWithoutSimulators);
					}
				}

				if (item.type == selectedRecipe.createItem.type)
					result = item;

				index++;
			}

			result ??= new Item(selectedRecipe.createItem.type, 0);
		}

		private static bool RecipeGroupMatch(Recipe recipe, int inventoryType, int requiredType)
		{
			foreach (int num in recipe.acceptedGroups)
			{
				RecipeGroup recipeGroup = RecipeGroup.recipeGroups[num];
				if (recipeGroup.ContainsItem(inventoryType) && recipeGroup.ContainsItem(requiredType))
					return true;
			}

			return false;

			//return recipe.useWood(type1, type2) || recipe.useSand(type1, type2) || recipe.useIronBar(type1, type2) || recipe.useFragment(type1, type2) || recipe.AcceptedByItemGroups(type1, type2) || recipe.usePressurePlate(type1, type2);
		}

		internal static void SetSelectedRecipe(Recipe recipe)
		{
			ArgumentNullException.ThrowIfNull(recipe);

			if (RecursiveCraftIntegration.Enabled)
			{
				int index;
				if (selectedRecipe != null && RecursiveCraftIntegration.IsCompoundRecipe(selectedRecipe) && selectedRecipe != recipe)
				{
					Recipe overridden = RecursiveCraftIntegration.GetOverriddenRecipe(selectedRecipe);
					if (overridden != recipe)
					{
						index = recipes.IndexOf(selectedRecipe);
						if (index != -1)
							recipes[index] = overridden;
					}
				}

				index = recipes.IndexOf(recipe);
				if (index != -1)
				{
					recipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);
					recipes[index] = recipe;
				}
			}

			selectedRecipe = recipe;
			RefreshStorageItems();
			blockStorageItems.Clear();
		}

		internal static void SlotFocusLogic()
		{
			if (result == null || result.IsAir || !Main.mouseItem.IsAir && (!ItemData.Matches(Main.mouseItem, result) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
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
					else
						Main.mouseItem.stack += withdrawn.stack;
					SoundEngine.PlaySound(SoundID.MenuTick);
					
					StorageGUI.needRefresh = true;
				}

				rightClickTimer--;
			}
		}

		private static void ResetSlotFocus()
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
							existing.stack += item.stack;
							item.stack = 0;

							fullyCompacted = true;
						} else {
							int diff = existing.maxStack - existing.stack;
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

			public List<Item> consumedItems;
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from the currently assigned Crafting Access.
		/// </summary>
		/// <param name="toCraft">How many items should be crafted</param>
		public static void Craft(int toCraft) {
			var sourceItems = storageItems.Where(item => !blockStorageItems.Contains(new ItemData(item))).ToList();
			var availableItems = sourceItems.Select(item => item.Clone()).ToList();
			var fromModule = storageItemsFromModules.Where((_, n) => !blockStorageItems.Contains(new ItemData(storageItems[n]))).ToList();
			List<Item> toWithdraw = new(), results = new();

			TEStorageHeart heart = GetHeart();

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			CraftingContext context = new() {
				sourceItems = sourceItems,
				availableItems = availableItems,
				toWithdraw = toWithdraw,
				results = results,
				sandbox = sandbox,
				consumedItems = new()
			};

			int target = toCraft;
			NetHelper.Report(true, "Attempting to craft " + toCraft + " items");

			while (toCraft > 0) {
				if (!AttemptSingleCraft(context))
					break;  // Could not craft any more items

				Item resultItem = selectedRecipe.createItem.Clone();
				toCraft -= resultItem.stack;

				resultItem.Prefix(-1);
				results.Add(resultItem);

				CatchDroppedItems = true;
				DroppedItems.Clear();

				RecipeLoader.OnCraft(resultItem, selectedRecipe, context.consumedItems);
				context.consumedItems.Clear();

				CatchDroppedItems = false;

				results.AddRange(DroppedItems);
			}

			NetHelper.Report(true, "Crafted " + (target - toCraft) + " items");

			if (target == toCraft) {
				//Could not craft anything, bail
				return;
			}

			toWithdraw = CompactItemList(toWithdraw);
			results = CompactItemList(results);

			if (Main.netMode == NetmodeID.SinglePlayer) {
				foreach (Item item in HandleCraftWithdrawAndDeposit(GetHeart(), toWithdraw, results))
					Main.LocalPlayer.QuickSpawnClonedItem(new EntitySource_TileEntity(GetHeart()), item, item.stack);
			} else if (Main.netMode == NetmodeID.MultiplayerClient)
				NetHelper.SendCraftRequest(GetHeart().Position, toWithdraw, results);
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
			foreach (Item tryItem in list) {
				if (reqType == tryItem.type || RecipeGroupMatch(selectedRecipe, tryItem.type, reqType)) {
					//Don't attempt to withdraw if the item is from a module, since it doesn't exist in the storage system anyway
					bool canWithdraw = addToWithdraw && !context.fromModule[listIndex];

					if (tryItem.stack > stack) {
						Item temp = tryItem.Clone();
						temp.stack = stack;

						if (canWithdraw)
							context.toWithdraw.Add(temp);
								
						tryItem.stack -= stack;
						stack = 0;
					} else {
						if (canWithdraw)
							context.toWithdraw.Add(tryItem.Clone());
								
						stack -= tryItem.stack;
						tryItem.stack = 0;
						tryItem.type = ItemID.None;
					}

					if (stack <= 0)
						break;
				}

				listIndex++;
			}

			return stack <= 0;
		}

		private static void ConsumeItemFromSource(CraftingContext context, int reqType, int stack) {
			foreach (Item tryItem in context.sourceItems) {
				if (reqType == tryItem.type || RecipeGroupMatch(selectedRecipe, tryItem.type, reqType)) {
					int origStack = stack;
					int stackToConsume;

					if (tryItem.stack > stack) {
						stackToConsume = stack;
						stack = 0;
					} else {
						stackToConsume = tryItem.stack;
						stack -= tryItem.stack;
					}

					foreach (var module in EnvironmentModuleLoader.modules)
						module.OnConsumeItemForRecipe(context.sandbox, tryItem, stackToConsume);

					Item consumed = tryItem.Clone();
					consumed.stack = stackToConsume;

					context.consumedItems.Add(consumed);

					tryItem.stack -= stackToConsume;

					if (tryItem.stack <= 0)
						tryItem.type = ItemID.None;

					if (stack <= 0)
						break;
				}
			}
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
			TEStorageHeart heart = GetHeart();
			heart.TryDeposit(item);
			return oldStack != item.stack;
		}

		internal static Item DoWithdrawResult(Item item, bool toInventory = false)
		{
			TEStorageHeart heart = GetHeart();
			Item withdrawn = heart.TryWithdraw(item, false, toInventory);

			if (withdrawn.IsAir && numItemsWithoutSimulators > 0) {
				//Heart did not contain the item; try to withdraw from the module items
				List<Item> moduleItems = items.GetRange(numItemsWithoutSimulators, items.Count - numItemsWithoutSimulators);

				TEStorageUnit.WithdrawFromItemCollection(moduleItems, item, out withdrawn, onItemRemoved: k => items.RemoveAt(k + numItemsWithoutSimulators));
			}

			return withdrawn;
		}
	}
}
