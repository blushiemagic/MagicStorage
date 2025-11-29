using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.UI;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Items;
using MagicStorage.Sorting;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

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

		private static void RefreshRecipes(CraftingRefreshThread thread) {
			if (thread.recipesToRefresh is not { Length: > 0 }) {
				// Refresh all recipes
				RefreshAllRecipes(thread);
			} else {
				RefreshSpecificRecipes(thread);

				forceSpecificRecipeResort = false;

				// Do a second pass when recursion crafting is enabled
				if (MagicStorageConfig.IsRecursionEnabled) {
					thread.recipesToRefresh = [.. thread.resultRecipes];
					RefreshSpecificRecipes(thread);
				}
			}

			NetHelper.Report(false, "Visible recipes: " + thread.resultRecipes.Count);
			NetHelper.Report(false, "Available recipes: " + thread.recipeIsAvailable.Count(static b => b));
		}

		private static void RefreshAllRecipes(CraftingRefreshThread thread)
		{
			NetHelper.Report(true, "Refreshing all recipes");

			thread.InitTaskSchedule(9, "Refreshing recipes");

			using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable))
				PopulateRecipes(thread, attempt: 0);

			bool didDefault = false;
			ref string errorText = ref thread.searchBarError;

			// now if nothing found we disable filters one by one
			if (thread.controls.fullSearchText.Trim().Length > 0)
			{
				if (thread.resultRecipes.Count == 0 && (thread.globalHiddenTypes.Count > 0 || thread.hiddenTypes.Count > 0))
				{
					NetHelper.Report(true, "No recipes passed the filter.  Attempting filter with no hidden recipes");

					// search hidden recipes too
					thread.globalHiddenTypes.Clear();
					thread.hiddenTypes.Clear();

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.CraftingNoBlacklist");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

					using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable))
						PopulateRecipes(thread, attempt: 1);
				}

				/*
				if (recipes.Count == 0 && filterMode != FilterMode.All)
				{
					// any category
					filterMode = FilterMode.All;
					DoFiltering(sortMode, filterMode, hiddenRecipes, favorited);
				}
				*/

				if (thread.resultRecipes.Count == 0 && thread.controls.modSearchOption != ModSearchBox.ModIndexAll)
				{
					NetHelper.Report(true, "No recipes passed the filter.  Attempting filter with All Mods setting");

					// search all mods
					thread.controls = thread.controls.CreateCopy(
						modSearchOptionOverride: ModSearchBox.ModIndexAll
					);

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.CraftingDefaultToAllMods");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

					using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable))
						PopulateRecipes(thread, attempt: 2);
				}
			}

			for (int i = 0; i < thread.resultRecipes.Count; i++) {
				Recipe recipe = thread.resultRecipes[i];
				bool available = thread.recipeIsAvailable[i];

				if (recipe?.Conditions.Count > 0)
					MagicUI.AddRefreshWatchdog(new RecipeWatchTarget(recipe), available);
			}

			if (!didDefault)
				errorText = null;
		}

		internal static void PopulateRecipes(CraftingRefreshThread thread, int attempt) {
			PopulateCollections(
				thread: thread,
				sortedAndFilteredObjects: ItemSorter.SortAndFilterRecipes(thread, attempt, provider: thread.recipeFilterProvider),
				destination: thread.resultRecipes,
				destinationAvailable: thread.recipeIsAvailable,
				isObjectAvailable: RefreshRecipes_IsAvailable_AlwaysCheckRecursion,
				objectNameForTask: "Recipes"
			);
		}

		internal static void PopulateCollections<T>(
			CraftingControlsRefreshThread thread,
			List<T> sortedAndFilteredObjects,
			List<T> destination,
			List<bool> destinationAvailable,
			Func<T, bool> isObjectAvailable,
			string objectNameForTask
		) {
			NetHelper.Report(true, "Retrieving objects from query...");

			destination.Clear();
			destinationAvailable.Clear();

			thread.InitTaskSchedule(sortedAndFilteredObjects.Count, "Processing " + objectNameForTask);

			var query = sortedAndFilteredObjects.NotifyStepsTo(thread).AsParallel().AsOrdered();

			if (thread.recipeFilterChoice == RecipeButtonsAvailableChoice) 
			{
				NetHelper.Report(true, "Filtering out only available objects...");

				destination.AddRange(query.Where(isObjectAvailable));

				destinationAvailable.AddRange(Enumerable.Repeat(true, destination.Count));
			}
			else
			{
				NetHelper.Report(true, "Checking all objects for availability...");

				destination.AddRange(sortedAndFilteredObjects);

				destinationAvailable.AddRange(query.Select(isObjectAvailable));
			}
		}

		private static bool RefreshRecipes_IsAvailable_AlwaysCheckRecursion(Recipe recipe) => IsAvailable(recipe);

		internal static bool forceSpecificRecipeResort;

		private static void RefreshSpecificRecipes(CraftingRefreshThread thread) {
			RefreshSpecificObjects(
				thread: thread,
				provider: thread.recipeFilterProvider,
				refreshingObjects: thread.recipesToRefresh,
				destination: thread.resultRecipes,
				destinationAvailable: thread.recipeIsAvailable,
				getItem: recipe => recipe.createItem,
				getItemType: recipe => recipe.createItem.type,
				canProcessObject: HiddenRecipes.IsVisible,
				isObjectAvailable: RefreshRecipes_IsAvailable_AlwaysCheckRecursion,
				objectNameForTask: "Recipes",
				forcedResort: ref forceSpecificRecipeResort
			);
		}

		internal static void RefreshSpecificObjects<T>(
			CraftingControlsRefreshThread thread,
			IFilterProvider<T> provider,
			IEnumerable<T> refreshingObjects,
			List<T> destination,
			List<bool> destinationAvailable,
			Func<T, Item> getItem,
			Func<T, int> getItemType,
			Func<T, bool> canProcessObject,
			Func<T, bool> isObjectAvailable,
			string objectNameForTask,
			ref bool forcedResort
		) {
			T[] toRefresh = refreshingObjects is T[] array ? array : [.. refreshingObjects];
			var recipeFilterChoice = thread.recipeFilterChoice;

			NetHelper.Report(true, $"Refreshing {toRefresh.Length} objects");

			thread.InitTaskSchedule(toRefresh.Length, "Processing " + objectNameForTask);

			// Assumes that the recipes are visible in the GUI
			bool needsResort = forcedResort;

			using var _ = FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable);

			foreach (T refreshingObject in toRefresh.NotifyStepsTo(thread)) {
				if (!canProcessObject(refreshingObject))
					continue;

				int objectAsItemType = getItemType(refreshingObject);
				if (!thread.controls.ItemPassesFilters(objectAsItemType))
					continue;

				int indexInResults = destination.IndexOf(refreshingObject);

				if (!isObjectAvailable(refreshingObject)) {
					if (indexInResults >= 0) {
						if (recipeFilterChoice == RecipeButtonsAvailableChoice) {
							// Available recipes; remove unavailable recipes
							destination.RemoveAt(indexInResults);
							destinationAvailable.RemoveAt(indexInResults);
						} else {
							// All recipes; mark as unavailable
							destinationAvailable[indexInResults] = false;
						}
					}
				} else {
					if (recipeFilterChoice == RecipeButtonsAvailableChoice) {
						if (indexInResults < 0 && ItemPassesCraftingFilters(thread, objectAsItemType)) {
							// Available recipes; add new recipes
							destination.Add(refreshingObject);
							destinationAvailable.Add(true);
							needsResort = true;
						}
					} else {
						if (indexInResults >= 0) {
							// All recipes; mark as available
							destinationAvailable[indexInResults] = true;
						}
					}
				}
			}

			if (needsResort) {
				// Sort the recipes
				thread.InitTaskSchedule(
					totalTasks: destination.Count,
					taskName: "Sorting " + objectNameForTask
				);

				var sortedObjects = ItemSorter.DoSorting(thread, destination, getItem);

				if (!thread.controls.showOnlyFavorites)
					sortedObjects = ItemSorter.OrderFavoritesFirst(sortedObjects, provider.IsFavorited);

				List<T> sortResults = [.. sortedObjects.NotifyStepsTo(thread)];

				destination.Clear();
				destination.AddRange(sortResults);

				destinationAvailable.Clear();
				destinationAvailable.AddRange(Enumerable.Repeat(true, destination.Count));
			}

			forcedResort = false;
		}

		private static bool IsRecipeValidForQuery(RefreshThread thread, Recipe recipe)
			=> recipe is not null && HiddenRecipes.IsVisible(recipe) && thread.controls.RecipePassesFilters(recipe);

		internal static bool ItemPassesCraftingFilters(CraftingControlsRefreshThread thread, int itemType)
			=> (!MagicStorageConfig.RecipeBlacklistEnabled || ((thread.recipeFilterChoice == RecipeButtonsBlacklistChoice) == thread.IsHidden(itemType)))
			&& (!MagicStorageConfig.CraftingFavoritingEnabled || thread.recipeFilterChoice != RecipeButtonsFavoritesChoice || thread.favoritedTypes.Contains(itemType));

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
