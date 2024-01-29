using MagicStorage.Common.Systems;
using MagicStorage.Common;
using MagicStorage.Components;
using MagicStorage.Items;
using MagicStorage.Sorting;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using static MagicStorage.CraftingGUI;
using MagicStorage.CrossMod;

namespace MagicStorage {
	partial class CraftingGUI {
		private class RecipeWatchTarget : IRefreshUIWatchTarget {
			private readonly Recipe _recipe;

			public RecipeWatchTarget(Recipe recipe) {
				_recipe = recipe;
			}

			public bool GetCurrentState() => IsAvailable(_recipe);

			public void OnStateChange(out bool forceFullRefresh) {
				SetNextDefaultRecipeCollectionToRefresh(new Recipe[] { _recipe });
				forceFullRefresh = false;
			}
		}

		internal static readonly List<Recipe> recipes = new();
		internal static readonly List<bool> recipeAvailable = new();

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

		private static void RefreshRecipes(StorageGUI.ThreadContext thread, CommonCraftingState state)
		{
			NetHelper.Report(true, "Refreshing all recipes");

			// Each DoFiltering does: GetRecipes, SortRecipes, adding recipes, adding recipe availability
			// Each GetRecipes does: loading base recipes, applying text/mod filters
			// Each SortRecipes does: DoSorting, blacklist filtering, favorite checks

			thread.InitTaskSchedule(9, "Refreshing recipes");

			var query = new Query<Recipe>(new QueryResults<Recipe>(recipes, recipeAvailable),
				ItemSorter.GetRecipes,
				SortRecipes,
				RefreshRecipes_IsAvailable_AlwaysCheckRecursion);

			using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable))
				DoFiltering(thread, state, query);

			bool didDefault = false;

			// now if nothing found we disable filters one by one
			if (thread.searchText.Length > 0)
			{
				if (recipes.Count == 0 && (state.globalHiddenTypes.Count > 0 || state.hiddenTypes.Count > 0))
				{
					NetHelper.Report(true, "No recipes passed the filter.  Attempting filter with no hidden recipes");

					// search hidden recipes too
					state.globalHiddenTypes = CommonCraftingState.EmptyGlobalHiddenTypes;
					state.hiddenTypes = ItemTypeOrderedSet.Empty;

					MagicUI.lastKnownSearchBarErrorReason = Language.GetTextValue("Mods.MagicStorage.Warnings.CraftingNoBlacklist");
					didDefault = true;

					thread.ResetTaskCompletion();

					using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable))
						DoFiltering(thread, state, query);
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

					using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable))
						DoFiltering(thread, state, query);
				}
			}

			for (int i = 0; i < recipes.Count; i++) {
				Recipe recipe = recipes[i];
				bool available = recipeAvailable[i];

				if (recipe?.Conditions.Count > 0)
					MagicUI.AddRefreshWatchdog(new RecipeWatchTarget(recipe), available);
			}

			if (!didDefault)
				MagicUI.lastKnownSearchBarErrorReason = null;
		}

		private static bool RefreshRecipes_IsAvailable_AlwaysCheckRecursion(Recipe recipe) => IsAvailable(recipe);

		internal static bool forceSpecificRecipeResort;

		private static void RefreshSpecificRecipes(StorageGUI.ThreadContext thread, ThreadState state) {
			var query = new SpecificQuery<Recipe>(new QueryResults<Recipe>(recipes, recipeAvailable),
				SortRecipes,
				RefreshRecipes_IsAvailable_AlwaysCheckRecursion,
				IsRecipeValidForQuery,
				CanBeAdded);

			RefreshSpecificQueryItems(thread, state, state.recipesToRefresh, query, forceSpecificRecipeResort, "recipes");

			forceSpecificRecipeResort = false;
		}

		private static bool IsRecipeValidForQuery(StorageGUI.ThreadContext thread, Recipe recipe) => recipe is not null && ItemSorter.RecipePassesFilter(recipe, thread);

		private static bool CanBeAdded(StorageGUI.ThreadContext thread, CommonCraftingState state, Recipe r)
			=> FilteringOptionLoader.Get(thread.filterMode).Filter(r.createItem) && DoesItemPassFilters(thread, state, r.createItem);

		private static IEnumerable<Recipe> SortRecipes(StorageGUI.ThreadContext thread, CommonCraftingState state, IEnumerable<Recipe> source) {
			IEnumerable<Recipe> sortedRecipes = ItemSorter.DoSorting(thread, source, r => r.createItem);

			thread.CompleteOneTask();

			// show only blacklisted recipes only if choice = 2, otherwise show all other
			if (MagicStorageConfig.RecipeBlacklistEnabled)
				sortedRecipes = sortedRecipes.Where(x => state.recipeFilterChoice == RecipeButtonsBlacklistChoice == state.IsHidden(x.createItem.type));

			thread.CompleteOneTask();

			// favorites first
			if (MagicStorageConfig.CraftingFavoritingEnabled) {
				sortedRecipes = sortedRecipes.Where(x => state.recipeFilterChoice != RecipeButtonsFavoritesChoice || state.favoritedTypes.Contains(x.createItem));
					
				sortedRecipes = sortedRecipes.OrderByDescending(r => state.favoritedTypes.Contains(r.createItem) ? 1 : 0);
			}

			thread.CompleteOneTask();

			return sortedRecipes;
		}

		private static void AnalyzeIngredients()
		{
			NetHelper.Report(true, "Analyzing crafting stations and environment requirements...");

			ResetZoneInfo();

			Player player = Main.LocalPlayer;

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

					TileLoader.AdjTiles(player, item.createTile);

					if (player.adjTile[TileID.WorkBenches] || player.adjTile[TileID.Tables] || player.adjTile[TileID.Tables2])
						player.adjTile[TileID.Chairs] = true;
					if (player.adjWater || TileID.Sets.CountsAsWaterSource[item.createTile])
						adjWater = true;
					if (player.adjLava || TileID.Sets.CountsAsLavaSource[item.createTile])
						adjLava = true;
					if (player.adjHoney || TileID.Sets.CountsAsHoneySource[item.createTile])
						adjHoney = true;
					if (player.adjShimmer || TileID.Sets.CountsAsShimmerSource[item.createTile])
						adjShimmer = true;
					if (player.alchemyTable || player.adjTile[TileID.AlchemyTable])
						alchemyTable = true;
					if (player.adjTile[TileID.Tombstones])
						graveyard = true;
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

			AdjustAndAssignZoneInfo();
		}

		internal static void ResetZoneInfo() {
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

			PlayerZoneCache.Cache();

			player.adjTile = adjTiles;
			player.adjWater = false;
			player.adjLava = false;
			player.adjHoney = false;
			player.alchemyTable = false;
		}

		internal static void AdjustAndAssignZoneInfo() {
			PlayerZoneCache.FreeCache(false);

			Player player = Main.LocalPlayer;

			TEStorageHeart heart = GetHeart();
			EnvironmentSandbox sandbox = new(player, heart);
			CraftingInformation information = ReadCraftingEnvironment();

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
			adjShimmer = information.shimmer;
			adjTiles = information.adjTiles;
		}
	}
}
