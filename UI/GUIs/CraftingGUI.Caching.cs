using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.Refreshing;
using System;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		private static int? amountCraftableForCurrentRecipe;
		private static Recipe recentRecipeAmountCraftable;

		private static bool? currentRecipeIsAvailable;
		private static Recipe recentRecipeAvailable;

		private static bool? currentRecipePassesBlock;
		private static Recipe recentRecipeBlock;
		
		private static CraftingSimulation simulatedCraftForCurrentRecipe;
		private static Recipe recentRecipeSimulation;
		private static int recentRecipeSimulationAmount;
		private static CraftingSimulationContext recentRecipeSimulationContext;
		private static readonly object inventoryCraftabilityGraphCacheLock = new();
		private static InventoryCraftabilityGraph cachedInventoryCraftabilityGraph;
		private static CraftingSimulationContext cachedInventoryCraftabilityGraphContext;
		private static bool hasCachedInventoryCraftabilityGraph;
		private const int CraftabilityGraphContextAmount = 0;
		private const int InfiniteRecursionCraftabilityGraphDepth = 5;
		private const int PartialCraftabilityGraphBuildThreshold = 64;
		private const int SelectedRecipePreviewCacheCapacity = 64;
		private const int SelectedRecipePreviewCacheVersion = 5;
		private const int AmountCraftableCacheCapacity = 512;
		private static readonly object selectedRecipePreviewCacheLock = new();
		private static readonly LinkedList<SelectedRecipePreviewCacheEntry> selectedRecipePreviewCacheLru = new();
		private static readonly Dictionary<SelectedRecipePreviewCacheKey, LinkedListNode<SelectedRecipePreviewCacheEntry>> selectedRecipePreviewCache = new();
		private static readonly object amountCraftableCacheLock = new();
		private static readonly LinkedList<AmountCraftableCacheEntry> amountCraftableCacheLru = new();
		private static readonly Dictionary<AmountCraftableCacheKey, LinkedListNode<AmountCraftableCacheEntry>> amountCraftableCache = new();

		public static void ResetRecentRecipeCache() {
			recentRecipeAvailable = null;
			currentRecipeIsAvailable = null;
			recentRecipeBlock = null;
			currentRecipePassesBlock = null;
			recentRecipeAmountCraftable = null;
			amountCraftableForCurrentRecipe = null;
			recentRecipeSimulation = null;
			recentRecipeSimulationAmount = 0;
			recentRecipeSimulationContext = default;
			simulatedCraftForCurrentRecipe = null;
		}

		internal static void ClearSelectedRecipePreviewCache() {
			lock (selectedRecipePreviewCacheLock) {
				selectedRecipePreviewCache.Clear();
				selectedRecipePreviewCacheLru.Clear();
			}

			ClearAmountCraftableCache();
		}

		internal static void InvalidateSelectedRecipePreviewAfterInventoryChange() {
			ResetRecentRecipeCache();
			ClearSelectedRecipePreviewCache();
			ResetInventoryCraftabilityGraphCache();

			if (selectedRecipe is not null) {
				recipeToAvailableLookup.Remove(selectedRecipe);
				recursionRecipeToAvailableSimulationLookup.Remove(selectedRecipe);
			}
		}

		private static void ClearAmountCraftableCache() {
			lock (amountCraftableCacheLock) {
				amountCraftableCache.Clear();
				amountCraftableCacheLru.Clear();
			}
		}

		private static void ResetInventoryCraftabilityGraphCache() {
			lock (inventoryCraftabilityGraphCacheLock) {
				cachedInventoryCraftabilityGraph = null;
				cachedInventoryCraftabilityGraphContext = default;
				hasCachedInventoryCraftabilityGraph = false;
			}
		}

		private static bool TryRestoreSelectedRecipePreviewCache(RecipeInfoPanelRefreshThread thread, Recipe recipe) {
			var timing = GetRefreshTiming(thread);
			bool restored = timing?.Measure(CraftingRefreshTimingPhase.SelectedPreviewCache, () => TryRestoreSelectedRecipePreviewCacheInner(thread, recipe))
				?? TryRestoreSelectedRecipePreviewCacheInner(thread, recipe);
			timing?.CountSelectedPreviewRestore(restored);
			return restored;
		}

		private static bool TryRestoreSelectedRecipePreviewCacheInner(RecipeInfoPanelRefreshThread thread, Recipe recipe) {
			if (recipe is null || thread.RecipeItems is not SingleResultRecipeItemsProvider recipeItems)
				return false;

			int amountToCraft = thread.CraftingObject.craftAmountTarget.Value;
			var key = CreateSelectedRecipePreviewCacheKey(thread, recipe, amountToCraft);
			SelectedRecipePreviewCacheEntry entry;

			lock (selectedRecipePreviewCacheLock) {
				if (!selectedRecipePreviewCache.TryGetValue(key, out var node))
					return false;

				selectedRecipePreviewCacheLru.Remove(node);
				selectedRecipePreviewCacheLru.AddFirst(node);
				entry = node.Value;
			}

			RestoreSelectedRecipePreviewCacheEntry(thread, recipeItems, entry);
			return true;
		}

		private static void StoreSelectedRecipePreviewCache(RecipeInfoPanelRefreshThread thread, Recipe recipe) {
			var timing = GetRefreshTiming(thread);
			timing?.Measure(CraftingRefreshTimingPhase.SelectedPreviewCache, () => StoreSelectedRecipePreviewCacheInner(thread, recipe));
			if (timing is null)
				StoreSelectedRecipePreviewCacheInner(thread, recipe);
		}

		private static void StoreSelectedRecipePreviewCacheInner(RecipeInfoPanelRefreshThread thread, Recipe recipe) {
			if (recipe is null || thread.RecipeItems is not SingleResultRecipeItemsProvider recipeItems)
				return;

			GetRefreshTiming(thread)?.CountSelectedPreviewStore();

			int amountToCraft = thread.CraftingObject.craftAmountTarget.Value;
			var key = CreateSelectedRecipePreviewCacheKey(thread, recipe, amountToCraft);
			var entry = CreateSelectedRecipePreviewCacheEntry(thread, recipeItems, key);
			if (!CanStoreSelectedRecipePreviewCacheEntry(recipe, entry))
				return;

			lock (selectedRecipePreviewCacheLock) {
				if (selectedRecipePreviewCache.TryGetValue(key, out var existing)) {
					existing.Value = entry;
					selectedRecipePreviewCacheLru.Remove(existing);
					selectedRecipePreviewCacheLru.AddFirst(existing);
					return;
				}

				var node = new LinkedListNode<SelectedRecipePreviewCacheEntry>(entry);
				selectedRecipePreviewCache[key] = node;
				selectedRecipePreviewCacheLru.AddFirst(node);

				while (selectedRecipePreviewCache.Count > SelectedRecipePreviewCacheCapacity) {
					var last = selectedRecipePreviewCacheLru.Last;
					if (last is null)
						break;

					selectedRecipePreviewCacheLru.RemoveLast();
					selectedRecipePreviewCache.Remove(last.Value.Key);
				}
			}
		}

		private static SelectedRecipePreviewCacheKey CreateSelectedRecipePreviewCacheKey(RecipeInfoPanelRefreshThread thread, Recipe recipe, int amountToCraft) {
			return new SelectedRecipePreviewCacheKey(
				SelectedRecipePreviewCacheVersion,
				recipe,
				CreateCraftingSimulationContext(thread, amountToCraft),
				thread.IngredientControls.showAllPossibleIngredients.Value
			);
		}

		private static SelectedRecipePreviewCacheEntry CreateSelectedRecipePreviewCacheEntry(RecipeInfoPanelRefreshThread thread, SingleResultRecipeItemsProvider recipeItems, SelectedRecipePreviewCacheKey key) {
			var ingredients = CloneItems(recipeItems.storedIngredients.items.Value);
			var info = new List<ItemInfo>(recipeItems.storedIngredients.info.Value);
			Item resultItem = recipeItems.resultItem.Value?.Clone();
			CraftingSimulation simulation = thread.RecipeSimulations.currentRecipeSimulation.Value;
			if (!IsSelectedRecipePreviewSimulationMatch(key, simulation))
				simulation = null;

			return new SelectedRecipePreviewCacheEntry(
				key,
				ingredients,
				info,
				resultItem,
				recipeItems.hasStorageResult,
				simulation,
				thread.storedItemsError,
				amountCraftableForCurrentRecipe ?? 0,
				currentRecipeIsAvailable ?? false,
				currentRecipePassesBlock ?? false
			);
		}

		private static bool CanStoreSelectedRecipePreviewCacheEntry(Recipe recipe, SelectedRecipePreviewCacheEntry entry) {
			if (!MagicStorageConfig.IsRecursionEnabled || !recipe.TryGetRecursiveRecipe(out _))
				return true;

			return entry.Simulation is { IsGraphBacked: true };
		}

		private static void RestoreSelectedRecipePreviewCacheEntry(RecipeInfoPanelRefreshThread thread, SingleResultRecipeItemsProvider recipeItems, SelectedRecipePreviewCacheEntry entry) {
			recipeItems.storedIngredients.items.Clear();
			recipeItems.storedIngredients.items.AddRange(CloneItems(entry.StoredIngredients));
			recipeItems.storedIngredients.info.Clear();
			recipeItems.storedIngredients.info.AddRange(entry.StoredIngredientInfo);
			recipeItems.resultItem.Value = entry.ResultItem?.Clone();
			recipeItems.hasStorageResult = entry.HasStorageResult;
			thread.RecipeSimulations.currentRecipeSimulation.Value = entry.Simulation;
			thread.storedItemsError = entry.StoredItemsError;

			thread.RestoreSelectedPreviewCaches(entry.Key.Context, entry.AmountCraftable, entry.IsAvailable, entry.PassesBlock);
		}

		private static bool IsSelectedRecipePreviewSimulationMatch(SelectedRecipePreviewCacheKey key, CraftingSimulation simulation) {
			return simulation is not null
				&& object.ReferenceEquals(simulation.Recipe, key.Recipe)
				&& simulation.RequestedAmount == key.Context.AmountToCraft
				&& simulation.Context == key.Context;
		}

		private static List<Item> CloneItems(List<Item> source) {
			List<Item> result = new(source.Count);

			foreach (Item item in source)
				result.Add(item?.Clone());

			return result;
		}

		private readonly record struct SelectedRecipePreviewCacheKey(
			int Version,
			Recipe Recipe,
			CraftingSimulationContext Context,
			bool ShowAllPossibleIngredients
		);

		private sealed record SelectedRecipePreviewCacheEntry(
			SelectedRecipePreviewCacheKey Key,
			List<Item> StoredIngredients,
			List<ItemInfo> StoredIngredientInfo,
			Item ResultItem,
			bool HasStorageResult,
			CraftingSimulation Simulation,
			string StoredItemsError,
			int AmountCraftable,
			bool IsAvailable,
			bool PassesBlock
		);

		private static bool TryGetCachedAmountCraftable<T>(T thread, Recipe recipe, out int amount)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, IRecipeSnapshotsProvider
		{
			amount = 0;
			if (thread?.RecipeSnapshots.ConditionsMet is null || recipe is null)
				return false;

			var key = new AmountCraftableCacheKey(recipe, CreateCraftingSimulationContext(thread, Item.CommonMaxStack));

			lock (amountCraftableCacheLock) {
				if (!amountCraftableCache.TryGetValue(key, out var node))
					return false;

				amountCraftableCacheLru.Remove(node);
				amountCraftableCacheLru.AddFirst(node);
				amount = node.Value.Amount;
				return true;
			}
		}

		private static void StoreCachedAmountCraftable<T>(T thread, Recipe recipe, int amount)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, IRecipeSnapshotsProvider
		{
			if (thread?.RecipeSnapshots.ConditionsMet is null || recipe is null)
				return;

			var key = new AmountCraftableCacheKey(recipe, CreateCraftingSimulationContext(thread, Item.CommonMaxStack));
			var entry = new AmountCraftableCacheEntry(key, amount);

			lock (amountCraftableCacheLock) {
				if (amountCraftableCache.TryGetValue(key, out var existing)) {
					existing.Value = entry;
					amountCraftableCacheLru.Remove(existing);
					amountCraftableCacheLru.AddFirst(existing);
					return;
				}

				var node = new LinkedListNode<AmountCraftableCacheEntry>(entry);
				amountCraftableCache[key] = node;
				amountCraftableCacheLru.AddFirst(node);

				while (amountCraftableCache.Count > AmountCraftableCacheCapacity) {
					var last = amountCraftableCacheLru.Last;
					if (last is null)
						break;

					amountCraftableCacheLru.RemoveLast();
					amountCraftableCache.Remove(last.Value.Key);
				}
			}
		}

		private readonly record struct AmountCraftableCacheKey(Recipe Recipe, CraftingSimulationContext Context);

		private sealed record AmountCraftableCacheEntry(AmountCraftableCacheKey Key, int Amount);

		private static void PrepareInventoryCraftabilityGraph<T>(T thread, bool buildIfCacheMiss)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
			=> PrepareInventoryCraftabilityGraph(thread, buildIfCacheMiss, focusedFrontierItemTypes: null, focusedSeedRecipes: null);

		private static void PrepareInventoryCraftabilityGraph<T>(T thread, bool buildIfCacheMiss, IReadOnlyCollection<int> focusedFrontierItemTypes, IEnumerable<Recipe> focusedSeedRecipes)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			thread.RecipeSimulations.inventoryCraftabilityGraph.Value = null;
			thread.RecipeSimulations.inventoryCraftabilityGraphContext.Value = default;

			if (!CanUseInventoryCraftabilityGraph(thread))
				return;

			var context = CreateCraftingSimulationContext(thread, CraftabilityGraphContextAmount);

			lock (inventoryCraftabilityGraphCacheLock) {
				if (hasCachedInventoryCraftabilityGraph && cachedInventoryCraftabilityGraphContext == context) {
					thread.RecipeSimulations.inventoryCraftabilityGraph.Value = cachedInventoryCraftabilityGraph;
					thread.RecipeSimulations.inventoryCraftabilityGraphContext.Value = context;
					return;
				}
			}

			if (!buildIfCacheMiss)
				return;

			var timing = GetRefreshTiming(thread);
			timing?.CountGraphBuild();

			bool focusedBuild = focusedFrontierItemTypes is { Count: > 0 };
			InventoryCraftabilityGraph graph = null;
			InventoryCraftabilityGraph graphToIncrement = null;
			bool graphCanUpdateSharedCache = !focusedBuild;
			var inventory = GetCurrentInventory(thread);

			if (focusedBuild) {
				lock (inventoryCraftabilityGraphCacheLock) {
					if (hasCachedInventoryCraftabilityGraph
					&& cachedInventoryCraftabilityGraph is { CanRejectMissingRecipes: true }
					&& CanIncrementInventoryCraftabilityGraph(cachedInventoryCraftabilityGraphContext, context)) {
						graphToIncrement = cachedInventoryCraftabilityGraph;
					}
				}

				if (graphToIncrement is not null)
					graph = timing?.Measure(CraftingRefreshTimingPhase.GraphBuild, () => graphToIncrement.UpdateIncremental(
						inventory,
						MagicCache.EnabledRecipes,
						GetInventoryCraftabilityGraphDepth(),
						focusedFrontierItemTypes,
						focusedSeedRecipes,
						thread.cancellationToken))
						?? graphToIncrement.UpdateIncremental(
							inventory,
							MagicCache.EnabledRecipes,
							GetInventoryCraftabilityGraphDepth(),
							focusedFrontierItemTypes,
							focusedSeedRecipes,
							thread.cancellationToken);

				if (graph is null)
					graph = timing?.Measure(CraftingRefreshTimingPhase.GraphBuild, () => InventoryCraftabilityGraph.BuildFocused(
						inventory,
						MagicCache.EnabledRecipes,
						GetInventoryCraftabilityGraphDepth(),
						focusedFrontierItemTypes,
						focusedSeedRecipes,
						thread.cancellationToken
					)) ?? InventoryCraftabilityGraph.BuildFocused(
						inventory,
						MagicCache.EnabledRecipes,
						GetInventoryCraftabilityGraphDepth(),
						focusedFrontierItemTypes,
						focusedSeedRecipes,
						thread.cancellationToken
					);

				graphCanUpdateSharedCache = false;
			} else
				graph = timing?.Measure(CraftingRefreshTimingPhase.GraphBuild, () => InventoryCraftabilityGraph.Build(
					inventory,
					MagicCache.EnabledRecipes,
					GetInventoryCraftabilityGraphDepth(),
					thread.cancellationToken
				)) ?? InventoryCraftabilityGraph.Build(
					inventory,
					MagicCache.EnabledRecipes,
					GetInventoryCraftabilityGraphDepth(),
					thread.cancellationToken
				);

			thread.RecipeSimulations.inventoryCraftabilityGraph.Value = graph;
			thread.RecipeSimulations.inventoryCraftabilityGraphContext.Value = context;

			if (!graphCanUpdateSharedCache)
				return;

			lock (inventoryCraftabilityGraphCacheLock) {
				if (focusedBuild
				&& hasCachedInventoryCraftabilityGraph
				&& !object.ReferenceEquals(cachedInventoryCraftabilityGraph, graphToIncrement))
					return;

				cachedInventoryCraftabilityGraph = graph;
				cachedInventoryCraftabilityGraphContext = context;
				hasCachedInventoryCraftabilityGraph = true;
			}
		}

		private static bool CanIncrementInventoryCraftabilityGraph(CraftingSimulationContext oldContext, CraftingSimulationContext newContext) {
			return oldContext.AmountToCraft == newContext.AmountToCraft
				&& oldContext.InfiniteItemsHash == newContext.InfiniteItemsHash
				&& oldContext.BlockedItemsHash == newContext.BlockedItemsHash
				&& oldContext.TilesHash == newContext.TilesHash
				&& oldContext.ConditionsHash == newContext.ConditionsHash
				&& oldContext.CreativeUnitPresent == newContext.CreativeUnitPresent
				&& oldContext.RecursionInfinite == newContext.RecursionInfinite
				&& oldContext.RecursionDepth == newContext.RecursionDepth;
		}

		private static void AttachCachedInventoryCraftabilityGraphIfAvailable<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			thread.RecipeSimulations.inventoryCraftabilityGraph.Value = null;
			thread.RecipeSimulations.inventoryCraftabilityGraphContext.Value = default;

			if (!CanUseInventoryCraftabilityGraph(thread))
				return;

			var context = CreateCraftingSimulationContext(thread, CraftabilityGraphContextAmount);

			lock (inventoryCraftabilityGraphCacheLock) {
				if (!hasCachedInventoryCraftabilityGraph || cachedInventoryCraftabilityGraphContext != context)
					return;

				thread.RecipeSimulations.inventoryCraftabilityGraph.Value = cachedInventoryCraftabilityGraph;
				thread.RecipeSimulations.inventoryCraftabilityGraphContext.Value = context;
			}
		}

		private static bool CanUseInventoryCraftabilityGraph<T>(T thread)
			where T : IIngredientControlsProvider
		{
			return MagicStorageConfig.IsRecursionEnabled
				&& MagicCache.EnabledRecipes is not null
				&& !thread.IngredientControls.creativeUnitPresent.Value;
		}

		private static int GetInventoryCraftabilityGraphDepth()
			=> MagicStorageConfig.IsRecursionInfinite
				? InfiniteRecursionCraftabilityGraphDepth
				: Math.Max(1, MagicStorageConfig.RecipeRecursionDepth + 1);

		private static bool ShouldBuildInventoryCraftabilityGraphForPartial<T>(T thread, bool refreshedInventorySnapshot)
			where T : IMainZoneObjectResultsProvider<Recipe>
		{
			if (thread.MainZoneObjectsResults.objectsToRefresh is not { Count: > 0 })
				return true;

			if (refreshedInventorySnapshot)
				return thread.MainZoneObjectsResults.objectsToRefresh.Count >= PartialCraftabilityGraphBuildThreshold;

			return false;
		}

		internal static void SetRecipeAndCraftingCaches<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider, IRecipeItemsProvider
		{
			var targetProvider = thread.CraftingObject.craftAmountTarget;

			if (thread.CraftingObject.selection.Value is not Recipe recipe) {
				targetProvider.Value = 1;
				return;
			}

			// NOTE: Crafting simulation and availability are automatically cached in the lookups when calling AmountCraftable() with a valid recursion recipe
			thread.InitTaskSchedule(
				totalTasks: 4,
				taskName: "Populating Caches"
			);

			recentRecipeAmountCraftable = recipe;
			recentRecipeAvailable = recipe;
			recentRecipeSimulation = recipe;
			recentRecipeBlock = recipe;

			int maxCraftable;
			int requestedTarget = targetProvider.Value;
			var restoredPreview = thread as RecipeInfoPanelRefreshThread;
			var cacheContext = CreateCraftingSimulationContext(thread, requestedTarget);
			bool canReusePreviewCaches = restoredPreview is { HasRestoredSelectedPreviewCaches: true }
				&& restoredPreview.RestoredSelectedPreviewContext == cacheContext;

			if (canReusePreviewCaches) {
				amountCraftableForCurrentRecipe = maxCraftable = restoredPreview.RestoredSelectedPreviewAmountCraftable;
				currentRecipeIsAvailable = restoredPreview.RestoredSelectedPreviewIsAvailable;
				currentRecipePassesBlock = restoredPreview.RestoredSelectedPreviewPassesBlock;
				thread.CraftObjectAvailableCache.lookup.AddOrUpdate(recipe, new Ref<bool>(currentRecipeIsAvailable.Value));
			} else if (TryGetSelectedRecipeSimulation(thread, recipe, requestedTarget, cacheContext, out var selectedSimulation)) {
				currentRecipeIsAvailable = selectedSimulation.AmountCrafted > 0;
				currentRecipePassesBlock = PassesBlock(thread, selectedSimulation);
				thread.CraftObjectAvailableCache.lookup.AddOrUpdate(recipe, new Ref<bool>(currentRecipeIsAvailable.Value));

				if (requestedTarget == 1)
					thread.RecipeSimulations.recipeToAvailableSimulation.AddOrUpdate(recipe, selectedSimulation);

				amountCraftableForCurrentRecipe = maxCraftable = currentRecipeIsAvailable.Value ? requestedTarget : 0;
			} else
				amountCraftableForCurrentRecipe = maxCraftable = AmountCraftable(thread, recipe);

			thread.CompleteOne();

			targetProvider.Value = Utils.Clamp(targetProvider.Value, 1, maxCraftable);
			if (targetProvider.Value != requestedTarget)
				canReusePreviewCaches = false;

			if (!canReusePreviewCaches)
				currentRecipeIsAvailable = IsAvailable(thread, recipe);

			thread.CompleteOne();

			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out var recursiveRecipe)) {
				var simulationProvider = thread.RecipeSimulations.currentRecipeSimulation;
				var context = CreateCraftingSimulationContext(thread, targetProvider.Value);

				if (simulationProvider.Value is not { } simulation
				|| !object.ReferenceEquals(simulation.Recipe, recipe)
				|| simulation.RequestedAmount != targetProvider.Value
				|| simulation.Context != context) {
					if (!TryGetCachedAvailabilitySimulation(thread, recipe, targetProvider.Value, context, out simulation)) {
						simulation = CreateCraftingSimulation(thread, recursiveRecipe, targetProvider.Value, GetCurrentInventory(thread), context);
					}

					simulationProvider.Value = simulation;
				}

				if (targetProvider.Value == 1)
					thread.RecipeSimulations.recipeToAvailableSimulation.AddOrUpdate(recipe, simulation);

				recentRecipeSimulationAmount = targetProvider.Value;
				recentRecipeSimulationContext = context;
				simulatedCraftForCurrentRecipe = simulation;
			} else
				simulatedCraftForCurrentRecipe = new CraftingSimulation();

			thread.CompleteOne();

			// Obsolete, but still needs to be processed
			recentRecipeBlock = recipe;
			if (!canReusePreviewCaches)
				currentRecipePassesBlock = simulatedCraftForCurrentRecipe is not null
					&& MagicStorageConfig.IsRecursionEnabled
					&& recipe.TryGetRecursiveRecipe(out _)
						? PassesBlock(thread, simulatedCraftForCurrentRecipe)
						: PassesBlock(thread, recipe);

			thread.CompleteOne();

			thread.CraftingObject.CopyToStaticFields();
		}

		private static bool TryGetSelectedRecipeSimulation<T>(T thread, Recipe recipe, int amountToCraft, CraftingSimulationContext context, out CraftingSimulation simulation)
			where T : RefreshThread, IRecipeSimulationsProvider
		{
			simulation = null;

			if (thread?.RecipeSimulations.currentRecipeSimulation.Value is not { } cached)
				return false;

			if (!object.ReferenceEquals(cached.Recipe, recipe)
			|| cached.RequestedAmount != amountToCraft
			|| cached.Context != context)
				return false;

			simulation = cached;
			return true;
		}

		private static bool TryGetCachedAvailabilitySimulation<T>(T thread, Recipe recipe, int amountToCraft, CraftingSimulationContext context, out CraftingSimulation simulation)
			where T : RefreshThread, IRecipeSimulationsProvider
		{
			simulation = null;
			if (amountToCraft != 1)
				return false;

			var lookup = thread.RecipeSimulations.recipeToAvailableSimulation.Value;
			if (!lookup.TryGetValue(recipe, out var cached)
			|| !object.ReferenceEquals(cached.Recipe, recipe)
			|| cached.RequestedAmount != amountToCraft
			|| cached.Context != context)
				return false;

			simulation = cached;
			return true;
		}

		private static bool TryGetCachedStaticAvailabilitySimulation(Recipe recipe, int amountToCraft, CraftingSimulationContext context, out CraftingSimulation simulation) {
			simulation = null;
			if (amountToCraft != 1)
				return false;

			if (!recursionRecipeToAvailableSimulationLookup.TryGetValue(recipe, out var cached)
			|| !object.ReferenceEquals(cached.Recipe, recipe)
			|| cached.RequestedAmount != amountToCraft
			|| cached.Context != context)
				return false;

			simulation = cached;
			return true;
		}

		private static bool TryRunGraphBackedSimulation<T>(T thread, RecursiveRecipe recursiveRecipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context, out CraftingSimulation simulation)
			where T : RefreshThread, IRecipeSimulationsProvider
		{
			simulation = null;

			if (thread?.RecipeSimulations.inventoryCraftabilityGraph.Value is not { } graph
			|| !IsSameCraftingSnapshotIgnoringAmount(thread.RecipeSimulations.inventoryCraftabilityGraphContext.Value, context))
				return false;

			simulation = new CraftingSimulation();
			if (simulation.TryPlanCraftsWithGraph(recursiveRecipe, amountToCraft, available, graph, context, thread.cancellationToken))
				return true;

			simulation = null;
			return false;
		}

		private static bool TryRunGraphBackedSimulation(RecursiveRecipe recursiveRecipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context, out CraftingSimulation simulation) {
			simulation = null;
			InventoryCraftabilityGraph graph;
			CraftingSimulationContext graphContext;

			lock (inventoryCraftabilityGraphCacheLock) {
				if (!hasCachedInventoryCraftabilityGraph) {
					graph = null;
					graphContext = default;
				} else {
					graph = cachedInventoryCraftabilityGraph;
					graphContext = cachedInventoryCraftabilityGraphContext;
				}
			}

			if (graph is null || !IsSameCraftingSnapshotIgnoringAmount(graphContext, context))
				return false;

			simulation = new CraftingSimulation();
			if (simulation.TryPlanCraftsWithGraph(recursiveRecipe, amountToCraft, available, graph, context))
				return true;

			simulation = null;
			return false;
		}

		private static CraftingSimulation CreateCraftingSimulation<T>(T thread, RecursiveRecipe recursiveRecipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context)
			where T : RefreshThread, IRecipeSimulationsProvider
		{
			var timing = GetRefreshTiming(thread);
			CraftingSimulation Create() {
				if (TryRunGraphBackedSimulation(thread, recursiveRecipe, amountToCraft, available, context, out var simulation))
					return simulation;

				simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recursiveRecipe, amountToCraft, available, context, thread?.cancellationToken ?? default);
				return simulation;
			}

			if (timing is null)
				return Create();

			timing.CountSelectedSimulationRun();
			return timing.Measure(CraftingRefreshTimingPhase.SelectedSimulation, Create);
		}

		private static CraftingSimulation CreateCraftingSimulation(RecursiveRecipe recursiveRecipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context) {
			if (TryRunGraphBackedSimulation(recursiveRecipe, amountToCraft, available, context, out var simulation))
				return simulation;

			simulation = new CraftingSimulation();
			simulation.SimulateCrafts(recursiveRecipe, amountToCraft, available, context);
			return simulation;
		}

		private static bool IsSameCraftingSnapshotIgnoringAmount(CraftingSimulationContext graphContext, CraftingSimulationContext simulationContext) {
			return graphContext.InventoryHash == simulationContext.InventoryHash
				&& graphContext.InfiniteItemsHash == simulationContext.InfiniteItemsHash
				&& graphContext.BlockedItemsHash == simulationContext.BlockedItemsHash
				&& graphContext.TilesHash == simulationContext.TilesHash
				&& graphContext.ConditionsHash == simulationContext.ConditionsHash
				&& graphContext.CreativeUnitPresent == simulationContext.CreativeUnitPresent
				&& graphContext.RecursionInfinite == simulationContext.RecursionInfinite
				&& graphContext.RecursionDepth == simulationContext.RecursionDepth;
		}

		public static int AmountCraftableForCurrentRecipe() {
			if (!hasCompleteData || selectedRecipe is null)
				return 0;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeAmountCraftable, selectedRecipe) && amountCraftableForCurrentRecipe is { } amount)
				return amount;

			// Calculate the value
			recentRecipeAmountCraftable = selectedRecipe;
			amountCraftableForCurrentRecipe = amount = AmountCraftable(selectedRecipe);
			return amount;
		}

		public static bool IsCurrentRecipeAvailable() {
			if (!hasCompleteData || selectedRecipe is null)
				return false;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeAvailable, selectedRecipe) && currentRecipeIsAvailable is { } available)
				return available;

			recipeToAvailableLookup.Remove(selectedRecipe);
			recursionRecipeToAvailableSimulationLookup.Remove(selectedRecipe);

			// Calculate the value
			recentRecipeAvailable = selectedRecipe;
			currentRecipeIsAvailable = available = IsAvailable(selectedRecipe);

			recipeToAvailableLookup.AddOrUpdate(selectedRecipe, new Ref<bool>(available));

			return available;
		}

		[Obsolete("The blocked ingredients check is now part of the recipe availability checks.", error: true)]
		public static bool DoesCurrentRecipePassIngredientBlock() {
			if (!hasCompleteData || selectedRecipe is null)
				return false;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeBlock, selectedRecipe) && currentRecipePassesBlock is { } available)
				return available;

			// Calculate the value
			recentRecipeBlock = selectedRecipe;
			currentRecipePassesBlock = available = PassesBlock(selectedRecipe);
			return available;
		}

		public static CraftingSimulation GetCraftingSimulationForCurrentRecipe() => GetCraftingSimulationForCurrentRecipe(craftAmountTarget);

		public static CraftingSimulation GetCraftingSimulationForCurrentRecipe(int amountToCraft) {
			if (!hasCompleteData || selectedRecipe is null)
				return new CraftingSimulation();

			if (object.ReferenceEquals(recentRecipeSimulation, selectedRecipe)
			&& recentRecipeSimulationAmount == amountToCraft
			&& simulatedCraftForCurrentRecipe is not null
			&& object.ReferenceEquals(simulatedCraftForCurrentRecipe.Recipe, selectedRecipe)
			&& simulatedCraftForCurrentRecipe.RequestedAmount == amountToCraft
			&& simulatedCraftForCurrentRecipe.Context == recentRecipeSimulationContext)
				return simulatedCraftForCurrentRecipe;

			var context = CreateCraftingSimulationContext(amountToCraft);

			recentRecipeSimulation = selectedRecipe;
			recentRecipeSimulationAmount = amountToCraft;
			recentRecipeSimulationContext = context;

			if (!MagicStorageConfig.IsRecursionEnabled || !selectedRecipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe))
				return simulatedCraftForCurrentRecipe = new CraftingSimulation();

			if (TryGetCachedStaticAvailabilitySimulation(selectedRecipe, amountToCraft, context, out CraftingSimulation cachedSimulation))
				return simulatedCraftForCurrentRecipe = cachedSimulation;

			// Calculate the value
			CraftingSimulation simulation = new CraftingSimulation();
			simulation = CreateCraftingSimulation(recursiveRecipe, amountToCraft, GetCurrentInventory(), context);
			simulatedCraftForCurrentRecipe = simulation;
			return simulation;
		}

		private static CraftingSimulationContext CreateCraftingSimulationContext(int amountToCraft)
			=> CreateCraftingSimulationContext(NullThread, amountToCraft);

		internal static CraftingSimulationContext CreateCurrentCraftingSimulationContextForDebug(int amountToCraft)
			=> CreateCraftingSimulationContext(amountToCraft);

		private static CraftingSimulationContext CreateCraftingSimulationContext<T>(T thread, int amountToCraft)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, IRecipeSnapshotsProvider
		{
			var blockedItems = thread?.IngredientControls.blockStorageItems.Value ?? blockStorageItems;
			HashSet<int> infiniteItems = thread?.IngredientControls.infiniteItems.Value ?? isItemInfinite;
			bool[] adjTiles = thread?.MainZoneObjectsFilterControls.adjTiles ?? CraftingGUI.adjTiles;
			bool[] conditionsMet = thread?.RecipeSnapshots.ConditionsMet;
			bool creativeUnitPresent = thread?.IngredientControls.creativeUnitPresent.Value ?? allItemsAreInfinite;
			int inventoryHash = blockedItems.Count <= 0
				? thread?.ProcessedStorageItems.itemCountsHash.Value ?? itemCountsHash.Value
				: GetCountsHash(GetItemCountsWithBlockedItemsRemoved(thread));
			int infiniteItemsHash = thread?.IngredientControls.InfiniteItemsHash ?? GetSetHash(infiniteItems);
			int blockedItemsHash = thread?.IngredientControls.BlockedItemsHash ?? GetBlockedItemsHash(blockedItems);
			int tilesHash = thread?.MainZoneObjectsFilterControls.AdjTilesHash ?? GetTrueIndicesHash(adjTiles);
			int conditionsHash = thread?.RecipeSnapshots.ConditionsHash ?? GetBoolArrayHash(conditionsMet);

			return new CraftingSimulationContext(
				amountToCraft,
				inventoryHash,
				infiniteItemsHash,
				blockedItemsHash,
				tilesHash,
				conditionsHash,
				creativeUnitPresent,
				MagicStorageConfig.IsRecursionInfinite,
				MagicStorageConfig.RecipeRecursionDepth
			);
		}

		private static int GetCountsHash(Dictionary<int, int> counts) {
			int hash = 0;

			foreach (var (type, stack) in counts)
				hash ^= HashCode.Combine(type, stack);

			return hash;
		}

		private static int GetSetHash(HashSet<int> values) {
			int hash = 0;

			foreach (int value in values)
				hash ^= HashCode.Combine(value);

			return hash;
		}

		private static int GetBlockedItemsHash(List<ItemData> items) {
			int hash = 0;

			foreach (ItemData item in items)
				hash ^= HashCode.Combine(item.Type, item.Prefix);

			return hash;
		}

		private static int GetTrueIndicesHash(bool[] values) {
			if (values is null)
				return 0;

			var hash = new HashCode();
			hash.Add(values.Length);

			for (int i = 0; i < values.Length; i++) {
				if (values[i])
					hash.Add(i);
			}

			return hash.ToHashCode();
		}

		private static int GetBoolArrayHash(bool[] values) {
			if (values is null)
				return 0;

			var hash = new HashCode();
			hash.Add(values.Length);

			for (int i = 0; i < values.Length; i++)
				hash.Add(values[i]);

			return hash.ToHashCode();
		}
	}
}
