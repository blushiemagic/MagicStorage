using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Components;
using MagicStorage.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class CraftingGUI {
		private class RecipeWatchTarget : IRefreshUIWatchTarget_2 {
			private readonly Recipe _recipe;

			public RecipeWatchTarget(Recipe recipe) {
				_recipe = recipe;
			}

			public bool GetCurrentState() {
				// Any ingredient or station change will cause a UI refresh, so only the conditions need to be checked
				IEnumerable<Condition> conditions = GetRecipeListConditionsToWatch(_recipe) ?? _recipe.Conditions;
				foreach (var condition in conditions) {
					if (!condition.IsMet())
						return false;
				}

				// Either all relevant conditions are met, or there are no conditions.
				return true;
			}

			public void OnStateChange(bool currentState) {
				RecipeSnapshots.ClearCachedConditions();
				SetNextDefaultRecipeCollectionToRefresh(new Recipe[] { _recipe });
				MagicUI.RequestMainZoneThread();
			}
		}

		internal static readonly List<Recipe> recipes = new();
		internal static readonly List<bool> recipeAvailable = new();
		internal static readonly ConditionalWeakTable<Recipe, Ref<bool>> recipeToAvailableLookup = new();
		internal static readonly ConditionalWeakTable<Recipe, CraftingSimulation> recursionRecipeToAvailableSimulationLookup = new();
		private static readonly Dictionary<Recipe, bool> recipeListExactAvailabilityCache = new(ReferenceEqualityComparer.Instance);
		private static readonly Dictionary<Recipe, Condition[]> recipeListConditionWatchLookup = new(ReferenceEqualityComparer.Instance);
		private static readonly object recipeListConditionWatchLock = new();
		private const int RecipeAvailabilityParallelismLimit = 8;

		private static void RefreshRecipes<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			if (thread.MainZoneObjectsResults.objectsToRefresh is not { Count: > 0 } refreshingObjects) {
				// Refresh all recipes
				RefreshAllRecipes(thread);
			} else {
				RefreshSpecificRecipes(thread);

				forceSpecificRecipeResort = false;

				// CHANGE: v0.7.1 - The second pass is handled by the code that populates "recipesToRefreshByIndex" instead
				/*
				// Do a second pass when recursion crafting is enabled
				if (MagicStorageConfig.IsRecursionEnabled) {
					thread.MainZoneObjectsResults.objectsToRefresh = [.. thread.MainZoneObjectsResults.objects];
					RefreshSpecificRecipes(thread);
				}
				*/
			}

			SetRecipeAvailableCache(thread);

			MagicUI.ClearRefreshWatchdogs();

			foreach (var (recipe, available) in thread.MainZoneObjectsResults.Enumerate()) {
				if (ShouldWatchRecipeConditions(recipe))
					MagicUI.AddRefreshWatchdog(new RecipeWatchTarget(recipe));
			}

			NetHelper.Report(false, "Visible recipes: " + thread.MainZoneObjectsResults.objects.Count);
			NetHelper.Report(false, "Available recipes: " + thread.MainZoneObjectsResults.objectIsAvailable.Count(static b => b));
		}

		private static void SetRecipeAvailableCache<T>(T thread)
			where T : RefreshThread, IMainZoneObjectResultsProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>
		{
			var lookup = thread.CraftObjectAvailableCache.lookup;
			if (thread.MainZoneObjectsResults.objectsToRefresh is { Count: > 0 })
				lookup.CopyFromStatic();
			else
				lookup.Clear();

			foreach (var (recipe, available) in thread.MainZoneObjectsResults.Enumerate()) {
				if (recipe is null)
					continue;

				if (recipeListExactAvailabilityCache.TryGetValue(recipe, out bool exactAvailable))
					lookup.Add(recipe, new Ref<bool>(exactAvailable));
			}

			recipeListExactAvailabilityCache.Clear();
		}

		private static void RefreshAllRecipes<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
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

			if (!didDefault)
				errorText = null;
		}

		internal static void PopulateRecipes<T>(T thread, int attempt)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			PopulateRecipesWithListAvailability(thread, ItemSorter.SortAndFilterRecipes(thread, attempt));
		}

		private static void PopulateRecipesWithListAvailability<T>(T thread, List<Recipe> sortedAndFilteredRecipes)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			var zoneResults = thread.MainZoneObjectsResults;
			var destination = zoneResults.objects;
			var destinationAvailable = zoneResults.objectIsAvailable;

			destination.Clear();
			destinationAvailable.Clear();
			recipeListExactAvailabilityCache.Clear();
			ClearRecipeListConditionsToWatch();

			thread.InitTaskSchedule(sortedAndFilteredRecipes.Count, "Processing Recipes");

			if (thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice == RecipeButtonsAvailableChoice) {
				NetHelper.Report(true, "Filtering out only available recipes...");

				foreach (var (recipe, availability) in EvaluateRecipeListAvailability(thread, sortedAndFilteredRecipes)) {
					if (!availability.IsAvailable)
						continue;

					destination.Add(recipe);
					destinationAvailable.Add(true);
					RememberRecipeListExactAvailability(recipe, availability);
				}
			} else {
				NetHelper.Report(true, "Checking all recipes for availability...");

				foreach (var (recipe, availability) in EvaluateRecipeListAvailability(thread, sortedAndFilteredRecipes)) {
					destination.Add(recipe);
					destinationAvailable.Add(availability.ShouldApplyToList && availability.IsAvailable);
					RememberRecipeListExactAvailability(recipe, availability);
				}
			}
		}

		private static (Recipe Recipe, RecipeListAvailabilityResult Availability)[] EvaluateRecipeListAvailability<T>(T thread, List<Recipe> sortedAndFilteredRecipes)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			var timing = GetRefreshTiming(thread);
			timing?.CountListQueries(sortedAndFilteredRecipes.Count);
			if (timing is not null)
				return timing.Measure(CraftingRefreshTimingPhase.ListAvailability, () => EvaluateRecipeListAvailabilityInner(thread, sortedAndFilteredRecipes));

			return EvaluateRecipeListAvailabilityInner(thread, sortedAndFilteredRecipes);
		}

		private static (Recipe Recipe, RecipeListAvailabilityResult Availability)[] EvaluateRecipeListAvailabilityInner<T>(T thread, List<Recipe> sortedAndFilteredRecipes)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			var results = new (Recipe Recipe, RecipeListAvailabilityResult Availability)[sortedAndFilteredRecipes.Count];
			var options = new ParallelOptions {
				CancellationToken = thread.cancellationToken,
				MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount - 1, 1, RecipeAvailabilityParallelismLimit)
			};

			try {
				Parallel.For(0, sortedAndFilteredRecipes.Count, options, index => {
					options.CancellationToken.ThrowIfCancellationRequested();

					Recipe recipe = sortedAndFilteredRecipes[index];
					results[index] = (recipe, IsAvailableForRecipeList(thread, recipe));
					thread.CompleteOne();
				});
			} catch (Exception ex) when (RefreshThread.IsCancellationException(ex)) {
				throw new OperationCanceledException("Recipe availability evaluation was cancelled.", ex, thread.cancellationToken);
			}

			return results;
		}

		private static void RememberRecipeListExactAvailability(Recipe recipe, RecipeListAvailabilityResult availability) {
			if (recipe is null || !availability.CanCacheExact)
				return;

			recipeListExactAvailabilityCache[recipe] = availability.IsAvailable;
			RememberRecipeListConditionsToWatch(recipe, availability.ConditionsToWatch);
		}

		private static void RememberRecipeListConditionsToWatch(Recipe recipe, Condition[] conditions) {
			lock (recipeListConditionWatchLock) {
				if (conditions is { Length: > 0 })
					recipeListConditionWatchLookup[recipe] = conditions;
				else
					recipeListConditionWatchLookup.Remove(recipe);
			}
		}

		private static IEnumerable<Condition> GetRecipeListConditionsToWatch(Recipe recipe) {
			lock (recipeListConditionWatchLock) {
				if (recipeListConditionWatchLookup.TryGetValue(recipe, out var conditions))
					return conditions;
			}

			return null;
		}

		private static void ClearRecipeListConditionsToWatch() {
			lock (recipeListConditionWatchLock)
				recipeListConditionWatchLookup.Clear();
		}

		private static bool ShouldWatchRecipeConditions(Recipe recipe) {
			if (recipe is null)
				return false;

			if (recipe.Conditions.Count > 0)
				return true;

			lock (recipeListConditionWatchLock)
				return recipeListConditionWatchLookup.ContainsKey(recipe);
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

			if (thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice == RecipeButtonsAvailableChoice) 
			{
				NetHelper.Report(true, "Filtering out only available objects...");

				foreach (T obj in sortedAndFilteredObjects.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
					if (!isObjectAvailable(thread, obj))
						continue;

					destination.Add(obj);
				}

				destinationAvailable.AddRange(Enumerable.Repeat(true, destination.Count));
			}
			else
			{
				NetHelper.Report(true, "Checking all objects for availability...");

				foreach (T obj in sortedAndFilteredObjects.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
					destination.Add(obj);
					destinationAvailable.Add(isObjectAvailable(thread, obj));
				}
			}
		}

		internal static bool forceSpecificRecipeResort;

		private static void RefreshSpecificRecipes<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			RefreshSpecificRecipesWithListAvailability(thread);
		}

		private static void RefreshSpecificRecipesWithListAvailability<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			var zoneResults = thread.MainZoneObjectsResults;
			var toRefresh = zoneResults.objectsToRefresh;
			var recipeFilterChoice = thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice;
			var zip = new TupleListProvider<Recipe, bool>(zoneResults.objects, zoneResults.objectIsAvailable);

			NetHelper.Report(true, $"Refreshing {toRefresh.Count} recipes");

			thread.InitTaskSchedule(toRefresh.Count, "Processing Recipes");

			bool needsResort = forceSpecificRecipeResort;
			var visited = new HashSet<int>();

			foreach (Recipe recipe in toRefresh.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				if (recipe is null || !visited.Add(RecipeToRecipeIndex(recipe)))
					continue;

				if (!HiddenRecipes.IsVisible(recipe))
					continue;

				int itemType = recipe.createItem.type;
				if (!thread.controls.ItemPassesFilters(itemType))
					continue;

				int indexInResults = zip.IndexOf(recipe);
				bool inList = indexInResults >= 0;

				var availability = IsAvailableForRecipeList(thread, recipe);
				if (!availability.ShouldApplyToList)
					continue;

				bool previousAvailable;
				bool available = availability.IsAvailable;
				RememberRecipeListExactAvailability(recipe, availability);

				if (recipeFilterChoice == RecipeButtonsAvailableChoice) {
					previousAvailable = inList;

					if (!available) {
						if (inList)
							zip.List.RemoveAt(indexInResults);
					} else if (!inList && ItemPassesCraftingFilters<T, Recipe>(thread, itemType)) {
						zip.Add(recipe, true);
						needsResort = true;
					}
				} else {
					previousAvailable = inList && zip.List[indexInResults].Item2;

					if (inList)
						zip.List[indexInResults].Item2 = available;
					else if (ItemPassesCraftingFilters<T, Recipe>(thread, itemType)) {
						zip.Add(recipe, available);
						needsResort = true;
					}
				}

				if (previousAvailable != available && GetPossibleRecursionDependents(recipe) is { } affected) {
					toRefresh.InsertRangeAfterCurrent(affected);
					thread.AdjustTaskTarget(toRefresh.Count);
				}
			}

			if (needsResort) {
				thread.InitTaskSchedule(
					totalTasks: zip.List.Count,
					taskName: "Sorting Recipes"
				);

				var sortedObjects = ItemSorter.DoSorting(thread, zip, zip.WrapFunction(static recipe => recipe.createItem));

				if (!thread.controls.showOnlyFavorites)
					sortedObjects = ItemSorter.OrderFavoritesFirst(sortedObjects, zip.WrapFunction(thread.MainZoneObjectsFilterControls.IsFavorited));

				zip.List = [.. sortedObjects.NotifyStepsTo(thread).WatchForCancellation(thread, 16)];
			}

			zip.CopyToProviders();

			NetHelper.Report(true, $"Refreshing finished.  Checked {visited.Count} recipes.");

			forceSpecificRecipeResort = false;
		}

		internal static void RefreshSpecificObjects<TThread, T>(
			TThread thread,
			Func<T, Item> getItem,
			Func<T, int> getItemType,
			Func<T, bool> canProcessObject,
			Func<TThread, T, bool> isObjectAvailable,
			Func<T, int> getIdentifier,
			Func<T, IEnumerable<T>> getAffectedObjects,
			string objectNameForTask,
			ref bool forcedResort
		)
			where TThread : RefreshThread, IMainZoneFilterControlsProvider<T>, IMainZoneObjectResultsProvider<T>
		{
			var zoneResults = thread.MainZoneObjectsResults;
			var toRefresh = zoneResults.objectsToRefresh;

			var recipeFilterChoice = thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice;

			var zip = new TupleListProvider<T, bool>(zoneResults.objects, zoneResults.objectIsAvailable);

			NetHelper.Report(true, $"Refreshing {toRefresh.Count} objects");

			thread.InitTaskSchedule(toRefresh.Count, "Processing " + objectNameForTask);

			// Assumes that the recipes are visible in the GUI
			bool needsResort = forcedResort;
			var visited = new HashSet<int>();

			foreach (T refreshingObject in toRefresh.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				if (!visited.Add(getIdentifier(refreshingObject)))
					continue;

				if (!canProcessObject(refreshingObject))
					continue;

				int objectAsItemType = getItemType(refreshingObject);
				if (!thread.controls.ItemPassesFilters(objectAsItemType))
					continue;

				int indexInResults = zip.IndexOf(refreshingObject);
				bool inList = indexInResults >= 0;

				bool previousAvailable;
				bool available = isObjectAvailable(thread, refreshingObject);

				if (recipeFilterChoice == RecipeButtonsAvailableChoice) {
					previousAvailable = inList;

					if (!available) {
						if (inList) {
							// Available recipes; remove unavailable recipes
							zip.List.RemoveAt(indexInResults);
						}
					} else {
						if (!inList && ItemPassesCraftingFilters<TThread, T>(thread, objectAsItemType)) {
							// Available recipes; add new recipes
							zip.Add(refreshingObject, true);
							needsResort = true;
						}
					}
				} else {
					previousAvailable = inList && zip.List[indexInResults].Item2;

					if (inList) {
						// All recipes; update the recipe entry in the list
						zip.List[indexInResults].Item2 = available;
					} else {
						if (ItemPassesCraftingFilters<TThread, T>(thread, objectAsItemType)) {
							// All recipes; add missing recipes
							zip.Add(refreshingObject, available);
							needsResort = true;
						}
					}
				}

				// The object's availability has changed; add the affected objects to the refreshing list
				if (previousAvailable != available && getAffectedObjects(refreshingObject) is { } affected) {
					toRefresh.InsertRangeAfterCurrent(affected);
					thread.AdjustTaskTarget(toRefresh.Count);
				}
			}

			if (needsResort) {
				// Sort the recipes
				thread.InitTaskSchedule(
					totalTasks: zip.List.Count,
					taskName: "Sorting " + objectNameForTask
				);

				var sortedObjects = ItemSorter.DoSorting(thread, zip, zip.WrapFunction(getItem));

				if (!thread.controls.showOnlyFavorites)
					sortedObjects = ItemSorter.OrderFavoritesFirst(sortedObjects, zip.WrapFunction(thread.MainZoneObjectsFilterControls.IsFavorited));

				zip.List = [.. sortedObjects.NotifyStepsTo(thread).WatchForCancellation(thread, 16)];
			}

			zip.CopyToProviders();

			NetHelper.Report(true, $"Refreshing finished.  Checked {visited.Count} objects.");

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
