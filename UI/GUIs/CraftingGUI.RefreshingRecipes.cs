using MagicStorage.Common.Systems;
using MagicStorage.Common;
using MagicStorage.Components;
using MagicStorage.Items;
using MagicStorage.Sorting;
using MagicStorage.UI.States;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static List<Recipe> recipes = new();
		internal static List<bool> recipeAvailable = new();

		private static void SafelyRefreshRecipes(StorageGUI.ThreadContext thread, ThreadState state) {
			try {
				if (state.recipesToRefresh is null)
					RefreshRecipes(thread, state);  //Refresh all recipes
				else {
					RefreshSpecificRecipes(thread, state);

					forceSpecificRecipeResort = false;

					// Do a second pass when recursion crafting is enabled
					if (MagicStorageConfig.IsRecursionEnabled) {
						state.recipesToRefresh = recipes.ToArray();
						RefreshSpecificRecipes(thread, state);
					}
				}

				NetHelper.Report(false, "Visible recipes: " + recipes.Count);
				NetHelper.Report(false, "Available recipes: " + recipeAvailable.Count(static b => b));

				NetHelper.Report(true, "Recipe refreshing finished");
			} catch (Exception e) {
				Main.QueueMainThreadAction(() => Main.NewTextMultiline(e.ToString(), c: Color.White));
			}
		}

		private static void RefreshRecipes(StorageGUI.ThreadContext thread, ThreadState state)
		{
			NetHelper.Report(true, "Refreshing all recipes");

			// Each DoFiltering does: GetRecipes, SortRecipes, adding recipes, adding recipe availability
			// Each GetRecipes does: loading base recipes, applying text/mod filters
			// Each SortRecipes does: DoSorting, blacklist filtering, favorite checks

			thread.InitTaskSchedule(9, "Refreshing recipes");

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

					thread.ResetTaskCompletion();

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

					thread.ResetTaskCompletion();

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

				thread.CompleteOneTask();

				NetHelper.Report(true, "Sorting recipes...");

				IEnumerable<Recipe> sortedRecipes = SortRecipes(thread, state, filteredRecipes);

				thread.CompleteOneTask();

				recipes.Clear();
				recipeAvailable.Clear();
				
				// For some reason, the loading text likes to hide itself here...
				MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting")?.RequestThreadWait(waiting: true);

				using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable)) {
					if (state.recipeFilterChoice == RecipeButtonsAvailableChoice)
					{
						NetHelper.Report(true, "Filtering out only available recipes...");

						recipes.AddRange(sortedRecipes.Where(r => IsAvailable(r)));

						thread.CompleteOneTask();

						recipeAvailable.AddRange(Enumerable.Repeat(true, recipes.Count));

						thread.CompleteOneTask();
					}
					else
					{
						recipes.AddRange(sortedRecipes);

						thread.CompleteOneTask();

						recipeAvailable.AddRange(recipes.AsParallel().AsOrdered().Select(r => IsAvailable(r)));

						thread.CompleteOneTask();
					}
				}

				// For some reason, the loading text likes to hide itself here...
				MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting")?.RequestThreadWait(waiting: true);
			} catch when (thread.token.IsCancellationRequested) {
				recipes.Clear();
				recipeAvailable.Clear();
			}
		}

		internal static bool forceSpecificRecipeResort;

		private static void RefreshSpecificRecipes(StorageGUI.ThreadContext thread, ThreadState state) {
			NetHelper.Report(true, "Refreshing " + state.recipesToRefresh.Length + " recipes");

			// Task count: N recipes, 3 tasks from SortRecipes, adding recipes, adding recipe availability
			thread.InitTaskSchedule(state.recipesToRefresh.Length + 5, $"Refreshing {state.recipesToRefresh.Length} recipes");

			//Assumes that the recipes are visible in the GUI
			bool needsResort = forceSpecificRecipeResort;

			foreach (Recipe recipe in state.recipesToRefresh) {
				if (recipe is null) {
					thread.CompleteOneTask();
					continue;
				}

				if (!ItemSorter.RecipePassesFilter(recipe, thread)) {
					thread.CompleteOneTask();
					continue;
				}

				int index = recipes.IndexOf(recipe);

				using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable)) {
					if (!IsAvailable(recipe)) {
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
							if (index < 0 && CanBeAdded(thread, state, recipe)) {
								//Add the recipe
								recipes.Add(recipe);
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

				thread.CompleteOneTask();
			}

			if (needsResort) {
				var sorted = new List<Recipe>(recipes)
					.AsParallel()
					.AsOrdered();

				IEnumerable<Recipe> sortedRecipes = SortRecipes(thread, state, sorted);

				recipes.Clear();
				recipeAvailable.Clear();

				recipes.AddRange(sortedRecipes);

				thread.CompleteOneTask();

				recipeAvailable.AddRange(Enumerable.Repeat(true, recipes.Count));

				thread.CompleteOneTask();
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

			thread.CompleteOneTask();

			// show only blacklisted recipes only if choice = 2, otherwise show all other
			if (MagicStorageConfig.RecipeBlacklistEnabled)
				sortedRecipes = sortedRecipes.Where(x => state.recipeFilterChoice == RecipeButtonsBlacklistChoice == state.hiddenRecipes.Contains(x.createItem));

			thread.CompleteOneTask();

			// favorites first
			if (MagicStorageConfig.CraftingFavoritingEnabled) {
				sortedRecipes = sortedRecipes.Where(x => state.recipeFilterChoice != RecipeButtonsFavoritesChoice || state.favoritedRecipes.Contains(x.createItem));
					
				sortedRecipes = sortedRecipes.OrderByDescending(r => state.favoritedRecipes.Contains(r.createItem) ? 1 : 0);
			}

			thread.CompleteOneTask();

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
					case ItemID.BottomlessHoneyBucket:
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
	}
}
