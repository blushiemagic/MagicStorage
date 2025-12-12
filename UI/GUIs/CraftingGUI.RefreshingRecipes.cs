using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Components;
using MagicStorage.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
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
		internal static readonly ConditionalWeakTable<Recipe, Ref<bool>> recipeToAvailableLookup = new();

		private static void RefreshRecipes<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSnapshotsProvider
		{
			if (thread.MainZoneObjectsResults.objectsToRefresh is not { Length: > 0 } refreshingObjects) {
				// Refresh all recipes
				RefreshAllRecipes(thread);
			} else {
				RefreshSpecificRecipes(thread);

				forceSpecificRecipeResort = false;

				// CHANGE: v0.7.0.12 - The second pass is handled by the code that populates "recipesToRefreshByIndex" instead
				/*
				// Do a second pass when recursion crafting is enabled
				if (MagicStorageConfig.IsRecursionEnabled) {
					thread.MainZoneObjectsResults.objectsToRefresh = [.. thread.MainZoneObjectsResults.objects];
					RefreshSpecificRecipes(thread);
				}
				*/
			}

			SetRecipeAvailableCache(thread);

			NetHelper.Report(false, "Visible recipes: " + thread.MainZoneObjectsResults.objects.Count);
			NetHelper.Report(false, "Available recipes: " + thread.MainZoneObjectsResults.objectIsAvailable.Count(static b => b));
		}

		private static void SetRecipeAvailableCache<T>(T thread)
			where T : RefreshThread, IMainZoneObjectResultsProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>
		{
			var lookup = thread.CraftObjectAvailableCache.lookup;
			lookup.Clear();

			foreach (var (recipe, available) in thread.MainZoneObjectsResults.Enumerate())
				lookup.Add(recipe, new Ref<bool>(available));
		}

		private static void RefreshAllRecipes<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSnapshotsProvider
		{
			NetHelper.Report(true, "Refreshing all recipes");

			thread.InitTaskSchedule(9, "Refreshing recipes");

		//	using (FlagSwitch.Create(ref disableNetPrintingForIsAvailable, true))
			PopulateRecipes(thread, attempt: 0);

			bool didDefault = false;
			ref string errorText = ref thread.searchBarError;

			// now if nothing found we disable filters one by one
			if (thread.controls.fullSearchText.Trim().Length > 0)
			{
				var controls = thread.MainZoneObjectsFilterControls;
				var results = thread.MainZoneObjectsResults.objects;

				if (results.Count == 0 && (controls.globalHiddenTypes.Count > 0 || controls.hiddenTypes.Count > 0))
				{
					NetHelper.Report(true, "No recipes passed the filter.  Attempting filter with no hidden recipes");

					// search hidden recipes too
					controls.globalHiddenTypes.Clear();
					controls.hiddenTypes.Clear();

					string error = Language.GetTextValue("Mods.MagicStorage.Warnings.CraftingNoBlacklist");

					if (errorText.Length > 0)
						errorText += $"\n{error}";
					else
						errorText = error;

					didDefault = true;

				//	using (FlagSwitch.Create(ref disableNetPrintingForIsAvailable, true))
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

				if (results.Count == 0 && thread.controls.modSearchOption != ModSearchBox.ModIndexAll)
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

				//	using (FlagSwitch.Create(ref disableNetPrintingForIsAvailable, true))
					PopulateRecipes(thread, attempt: 2);
				}
			}

			foreach (var (recipe, available) in thread.MainZoneObjectsResults.Enumerate()) {
				if (recipe?.Conditions.Count > 0)
					MagicUI.AddRefreshWatchdog(new RecipeWatchTarget(recipe), available);
			}

			if (!didDefault)
				errorText = null;
		}

		internal static void PopulateRecipes<T>(T thread, int attempt)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSnapshotsProvider
		{
			PopulateCollections(
				thread,
				ItemSorter.SortAndFilterRecipes(thread, attempt),
				IsAvailable,
				"Recipes"
			);
		}

		internal static void PopulateCollections<TThread, T>(
			TThread thread,
			List<T> sortedAndFilteredObjects,
			Func<TThread, T, bool> isObjectAvailable,
			string objectNameForTask
		)
			where TThread : RefreshThread, IMainZoneFilterControlsProvider, IMainZoneObjectResultsProvider<T>
		{
			var zoneResults = thread.MainZoneObjectsResults;
			var destination = zoneResults.objects;
			var destinationAvailable = zoneResults.objectIsAvailable;

			destination.Clear();
			destinationAvailable.Clear();

			thread.InitTaskSchedule(sortedAndFilteredObjects.Count, "Processing " + objectNameForTask);

			var query = sortedAndFilteredObjects.NotifyStepsTo(thread).ToCancellableOrderedQuery(thread, 16);

			if (thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice == RecipeButtonsAvailableChoice) 
			{
				NetHelper.Report(true, "Filtering out only available objects...");

				foreach (var obj in query) {
					if (isObjectAvailable(thread, obj))
						destination.Add(obj);
				}

				destinationAvailable.AddRange(Enumerable.Repeat(true, destination.Count));
			}
			else
			{
				NetHelper.Report(true, "Checking all objects for availability...");

				destination.AddRange(sortedAndFilteredObjects);

				foreach (var obj in query)
					destinationAvailable.Add(isObjectAvailable(thread, obj));
			}
		}

		internal static bool forceSpecificRecipeResort;

		private static void RefreshSpecificRecipes<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSnapshotsProvider
		{
			RefreshSpecificObjects(
				thread,
				static (Recipe recipe) => recipe.createItem,
				static (Recipe recipe) => recipe.createItem.type,
				HiddenRecipes.IsVisible,
				IsAvailable,
				"Recipes",
				ref forceSpecificRecipeResort
			);
		}

		internal static void RefreshSpecificObjects<TThread, T>(
			TThread thread,
			Func<T, Item> getItem,
			Func<T, int> getItemType,
			Func<T, bool> canProcessObject,
			Func<TThread, T, bool> isObjectAvailable,
			string objectNameForTask,
			ref bool forcedResort
		)
			where TThread : RefreshThread, IMainZoneFilterControlsProvider<T>, IMainZoneObjectResultsProvider<T>
		{
			var zoneResults = thread.MainZoneObjectsResults;
			var toRefresh = zoneResults.objectsToRefresh;
			var destination = zoneResults.objects;
			var destinationAvailable = zoneResults.objectIsAvailable;

			var recipeFilterChoice = thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice;

			NetHelper.Report(true, $"Refreshing {toRefresh.Length} objects");

			thread.InitTaskSchedule(toRefresh.Length, "Processing " + objectNameForTask);

			// Assumes that the recipes are visible in the GUI
			bool needsResort = forcedResort;

			foreach (T refreshingObject in toRefresh.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				if (!canProcessObject(refreshingObject))
					continue;

				int objectAsItemType = getItemType(refreshingObject);
				if (!thread.controls.ItemPassesFilters(objectAsItemType))
					continue;

				int indexInResults = destination.IndexOf(refreshingObject);

				if (!isObjectAvailable(thread, refreshingObject)) {
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
						if (indexInResults < 0 && ItemPassesCraftingFilters<TThread, T>(thread, objectAsItemType)) {
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
					sortedObjects = ItemSorter.OrderFavoritesFirst(sortedObjects, thread.MainZoneObjectsFilterControls.IsFavorited);

				List<T> sortResults = [.. sortedObjects.NotifyStepsTo(thread).WatchForCancellation(thread, 16)];

				destination.Clear();
				destination.AddRange(sortResults);

				destinationAvailable.Clear();

				if (recipeFilterChoice == RecipeButtonsAvailableChoice) {
					// The remaining recipes are all available, so just ensure that all entries are "true"
					destinationAvailable.AddRange(Enumerable.Repeat(true, destination.Count));
				} else {
					// Check if each recipe is available
					// If "isObjectAvailable" is using a result lookup, this step is fast
					thread.InitTaskSchedule(
						totalTasks: destination.Count,
						taskName: "Re-evaluating Recipes"
					);

					foreach (T obj in destination.NotifyStepsTo(thread).WatchForCancellation(thread, 16))
						destinationAvailable.Add(isObjectAvailable(thread, obj));
				}
			}

			forcedResort = false;
		}

		internal static bool ItemPassesCraftingFilters<TControls, T>(TControls thread, int itemType)
			where TControls : IMainZoneFilterControlsProvider<T>
		{
			var controls = thread.MainZoneObjectsFilterControls;

			return (!MagicStorageConfig.RecipeBlacklistEnabled || ((controls.zoneObjectFilterChoice == RecipeButtonsBlacklistChoice) == controls.IsHidden(itemType)))
				&& (!MagicStorageConfig.CraftingFavoritingEnabled || controls.zoneObjectFilterChoice != RecipeButtonsFavoritesChoice || controls.IsFavorited(itemType));
		}

		private static void AnalyzeIngredients()
		{
			NetHelper.Report(true, "Analyzing crafting stations and environment requirements...");

			ResetZoneInfo();

			CraftingInformation information = ReadCraftingEnvironment();

			foreach (Item item in GetCraftingStations())
			{
				Utility.AddCraftingZones(item, ref information);
			}

			adjTiles[ModContent.TileType<Components.CraftingAccess>()] = true;

			WriteCraftingEnvironment(information);

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
		}

		internal static void AdjustAndAssignZoneInfo() {
			Player player = Main.LocalPlayer;

			TEStorageHeart heart = GetHeart();
			EnvironmentSandbox sandbox = new(player, heart);
			CraftingInformation information = ReadCraftingEnvironment();

			if (heart is not null) {
				foreach (EnvironmentModule module in heart.GetModules())
					module.ModifyCraftingZones(sandbox, ref information);
			}

			WriteCraftingEnvironment(information);
		}
	}
}
