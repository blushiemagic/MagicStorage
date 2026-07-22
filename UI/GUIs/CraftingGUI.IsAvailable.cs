using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.Refreshing;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class CraftingGUI {
	//	[ThreadStatic]
	//	internal static bool disableNetPrintingForIsAvailable;

		/// <summary>
		/// Returns <see langword="true"/> if the current recipe is available and passes the "blocked ingredients" filter
		/// </summary>
		[Obsolete("This method is functionally identical to " + nameof(IsCurrentRecipeAvailable) + "().", error: true)]
		public static bool IsCurrentRecipeFullyAvailable() => IsCurrentRecipeAvailable();

		/// <inheritdoc cref="IsAvailable(Recipe, bool)"/>
		public static bool IsAvailable(Recipe recipe) => IsAvailable(recipe, true);

		/// <inheritdoc cref="IsAvailable{T}(T, Recipe, bool)"/>
		public static bool IsAvailable<T>(T thread, Recipe recipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			return IsAvailable(thread, recipe, true);
		}

		/// <summary>
		/// Checks whether the ingredient, crafting station and condition requirements for <paramref name="recipe"/> are met.<br/>
		/// <b>NOTE:</b> if a <see cref="RefreshThread"/> is currently active, this method will ignore its controls.
		/// </summary>
		/// <param name="recipe">The recipe.</param>
		/// <param name="checkRecursive">
		/// If <see langword="true"/>, checks availability using the full recursion tree of the recipe if recursion crafting is enabled.<br/>
		/// Defaults to <see langword="true"/>.
		/// </param>
		public static bool IsAvailable(Recipe recipe, bool checkRecursive = true) => IsAvailable(NullThread, recipe, checkRecursive);

		/// <summary>
		/// Checks whether the ingredient, crafting station and condition requirements for <paramref name="recipe"/> are met.
		/// </summary>
		/// <param name="thread">The <see cref="RefreshThread"/> from which to gather controls.</param>
		/// <param name="recipe">The recipe.</param>
		/// <param name="checkRecursive">
		/// If <see langword="true"/>, checks availability using the full recursion tree of the recipe if recursion crafting is enabled.<br/>
		/// Defaults to <see langword="true"/>.
		/// </param>
		public static bool IsAvailable<T>(T thread, Recipe recipe, bool checkRecursive)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			if (recipe is null)
				return false;

			var lookup = thread?.CraftObjectAvailableCache.lookup.Value ?? recipeToAvailableLookup;

			if (lookup.TryGetValue(recipe, out var valueByRef))
				return valueByRef.Value;

			/*
			if (!disableNetPrintingForIsAvailable) {
				NetHelper.Report(true, "Checking if recipe is available...");

				if (checkRecursive && MagicStorageConfig.IsRecursionEnabled)
					NetHelper.Report(false, "Calculating recursion tree for recipe...");
			}
			*/

			bool available = false;
			if (checkRecursive && MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				if (MagicUI.CurrentlyRefreshing)
					available = IsAvailable_CheckRecursiveRecipe(thread, recipe, recursiveRecipe);
				else
					available = ExecuteInCraftingGuiEnvironment(thread, recipe, recursiveRecipe, IsAvailable_CheckRecursiveRecipe);
			} else
				available = IsAvailable_CheckNormalRecipe(thread, recipe);

			lookup.AddOrUpdate(recipe, new Ref<bool>(available));

			/*
			if (!disableNetPrintingForIsAvailable)
				NetHelper.Report(true, $"Recipe {(available ? "was" : "was not")} available");
			*/

			return available;
		}

		// CHANGE: v0.7.1 - No longer has an "int ignoreItem" parameter that was used to ignore the recipe's result item
		private static bool IsAvailable_CheckRecursiveRecipe<T>(T thread, Recipe recipe, RecursiveRecipe recursiveRecipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, ICraftingObjectProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			var lookup = thread?.RecipeSimulations.recipeToAvailableSimulation.Value ?? recursionRecipeToAvailableSimulationLookup;
			var context = CreateCraftingSimulationContext(thread, 1);
			if (lookup.TryGetValue(recipe, out var simulation)
			&& object.ReferenceEquals(simulation.Recipe, recipe)
			&& simulation.RequestedAmount == 1
			&& simulation.Context == context)
				return simulation.AmountCrafted > 0;

			var probe = ProbeRecursiveAvailability(thread, recipe);
			if (probe.Kind == RecursiveAvailabilityProbeKind.DirectAvailable)
				return true;

			if (probe.Kind == RecursiveAvailabilityProbeKind.GraphRejected)
				return false;

		//	using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI)) {
			var availableObjects = GetCurrentInventory(thread);
			simulation = CreateCraftingSimulation(thread, recursiveRecipe, 1, availableObjects, context);
			lookup.AddOrUpdate(recipe, simulation);
			return simulation.AmountCrafted > 0;
		//	}
		}

		private static RecursiveAvailabilityProbeResult ProbeRecursiveAvailability<T>(T thread, Recipe recipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			if (thread?.RecipeSimulations.inventoryCraftabilityGraph.Value is { } graph) {
				var graphProbe = graph.ProbeRecipe(recipe);
				if (!graphProbe.HasCandidate && graph.CanRejectMissingRecipes)
					return RecursiveAvailabilityProbeResult.GraphRejected;

				if (graphProbe.IsDirectRecipeAuthority)
					return RecursiveAvailabilityProbeResult.DirectAvailable;

				if (IsAvailable_CheckNormalRecipe(thread, recipe))
					return RecursiveAvailabilityProbeResult.DirectAvailable;

				return RecursiveAvailabilityProbeResult.NeedsExactSimulation(graphProbe);
			}

			if (IsAvailable_CheckNormalRecipe(thread, recipe))
				return RecursiveAvailabilityProbeResult.DirectAvailable;

			return RecursiveAvailabilityProbeResult.NeedsExactSimulation(default);
		}

		private static RecipeListAvailabilityResult IsAvailableForRecipeList<T>(T thread, Recipe recipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			if (recipe is null)
				return RecipeListAvailabilityResult.Exact(false);

			if (!MagicStorageConfig.IsRecursionEnabled || !recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				bool available = IsAvailable_CheckNormalRecipe(thread, recipe);
				return RecipeListAvailabilityResult.Exact(available, GetConditionsToWatch(null, recipe));
			}

			var probe = ProbeRecursiveAvailability(thread, recipe);
			if (probe.Kind == RecursiveAvailabilityProbeKind.DirectAvailable)
				return RecipeListAvailabilityResult.Exact(true, GetConditionsToWatch(null, recipe));

			if (probe.Kind == RecursiveAvailabilityProbeKind.GraphRejected)
				return RecipeListAvailabilityResult.Exact(false, GetConditionsToWatch(null, recipe));

			if (probe.GraphProbe.HasCandidate) {
				var context = CreateCraftingSimulationContext(thread, 1);
				var availableObjects = GetCurrentInventory(thread);
				var simulation = new CraftingSimulation();
				if (thread.RecipeSimulations.inventoryCraftabilityGraph.Value is { } graph
				&& IsSameCraftingSnapshotIgnoringAmount(thread.RecipeSimulations.inventoryCraftabilityGraphContext.Value, context)
				&& simulation.TryPlanCraftsWithGraph(recursiveRecipe, 1, availableObjects, graph, context, thread.cancellationToken))
					return RecipeListAvailabilityResult.Exact(true, GetConditionsToWatch(simulation, recipe));

				return RecipeListAvailabilityResult.Exact(false, GetConditionsToWatch(null, recipe));
			}

			return RecipeListAvailabilityResult.UnknownUnavailable;
		}

		private readonly struct RecipeListAvailabilityResult {
			public static RecipeListAvailabilityResult UnknownUnavailable { get; } = new(false, canCacheExact: false, shouldApplyToList: false, conditionsToWatch: null);

			public bool IsAvailable { get; }

			public bool CanCacheExact { get; }

			public bool ShouldApplyToList { get; }

			public Condition[] ConditionsToWatch { get; }

			private RecipeListAvailabilityResult(bool isAvailable, bool canCacheExact, bool shouldApplyToList, Condition[] conditionsToWatch) {
				IsAvailable = isAvailable;
				CanCacheExact = canCacheExact;
				ShouldApplyToList = shouldApplyToList;
				ConditionsToWatch = conditionsToWatch;
			}

			public static RecipeListAvailabilityResult Exact(bool isAvailable, Condition[] conditionsToWatch = null) => new(isAvailable, canCacheExact: true, shouldApplyToList: true, conditionsToWatch);
		}

		private static Condition[] GetConditionsToWatch(CraftingSimulation simulation, Recipe fallbackRecipe) {
			if (simulation is not null) {
				var conditions = simulation.RequiredConditions.ToArray();
				if (conditions.Length > 0)
					return conditions;
			}

			return fallbackRecipe.Conditions.Count > 0 ? fallbackRecipe.Conditions.ToArray() : null;
		}

		private readonly struct RecursiveAvailabilityProbeResult {
			public static RecursiveAvailabilityProbeResult DirectAvailable { get; } = new(RecursiveAvailabilityProbeKind.DirectAvailable, default);

			public static RecursiveAvailabilityProbeResult GraphRejected { get; } = new(RecursiveAvailabilityProbeKind.GraphRejected, default);

			public RecursiveAvailabilityProbeKind Kind { get; }

			public InventoryCraftabilityRecipeProbe GraphProbe { get; }

			public InventoryCraftabilityProbeFlags GraphFlags => GraphProbe.Flags;

			private RecursiveAvailabilityProbeResult(RecursiveAvailabilityProbeKind kind, InventoryCraftabilityRecipeProbe graphProbe) {
				Kind = kind;
				GraphProbe = graphProbe;
			}

			public static RecursiveAvailabilityProbeResult NeedsExactSimulation(InventoryCraftabilityRecipeProbe graphProbe)
				=> new(RecursiveAvailabilityProbeKind.NeedsExactSimulation, graphProbe);
		}

		private enum RecursiveAvailabilityProbeKind {
			DirectAvailable,
			GraphRejected,
			NeedsExactSimulation
		}

		private static bool IsAvailable_CheckNormalRecipe<T>(T thread, Recipe recipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, IRecipeSnapshotsProvider
		{
			if (recipe is null)
				return false;

			// CHANGE: v0.7.1 - Condition checks are moved first to better optimize RecipeWatchTarget
			bool conditionsAvailable = thread?.RecipeSnapshots.ConditionsMet[recipe.RecipeIndex]
				?? ExecuteInCraftingGuiEnvironment(recipe, RecipeLoader.RecipeAvailable);

			if (!conditionsAvailable)
				return false;

			bool[] adjTiles = thread?.MainZoneObjectsFilterControls.adjTiles ?? CraftingGUI.adjTiles;

			foreach (int requiredTile in recipe.requiredTile) {
				if (!adjTiles[requiredTile])
					return false;
			}

			bool creativeUnitPresent = thread?.IngredientControls.creativeUnitPresent.Value ?? allItemsAreInfinite;
			HashSet<int> infiniteItems = thread?.IngredientControls.infiniteItems.Value ?? isItemInfinite;

			if (creativeUnitPresent)
				return true;

			var itemCountsDictionary = GetItemCountsWithBlockedItemsRemoved(thread);

			foreach (Item ingredient in recipe.requiredItem)
			{
				if (!TryGetIngredientQuantity(recipe, itemCountsDictionary, infiniteItems, ingredient.type, out int availableQuantity))
				{
					// Infinite item
					continue;
				}

				if (availableQuantity < ingredient.stack)
					return false;
			}

			return true;
		}

		internal static bool PassesBlock(Recipe recipe)
		{
			return PassesBlock(NullThread, recipe);
		}

		internal static bool PassesBlock<T>(T thread, Recipe recipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, IRecipeSnapshotsProvider, IRecipeItemsProvider, IRecipeSimulationsProvider
		{
			if (recipe is null)
				return false;

		//	NetHelper.Report(true, "Checking if recipe passes \"blocked ingredients\" check...");

			bool success;
			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				int amountToCraft = thread?.CraftingObject.craftAmountTarget.Value ?? craftAmountTarget;
				var context = CreateCraftingSimulationContext(thread, amountToCraft);

				var simulation = new CraftingSimulation();
				simulation = CreateCraftingSimulation(thread, recursiveRecipe, amountToCraft, GetCurrentInventory(thread), context);

				success = PassesBlock_CheckSimulation(thread, simulation);
			} else
				success = PassesBlock_CheckRecipe(thread, recipe);

		//	NetHelper.Report(true, $"Recipe {(success ? "passed" : "failed")} the ingredients check");
			return success;
		}

		internal static bool PassesBlock<T>(T thread, CraftingSimulation simulation)
			where T : RefreshThread, IIngredientControlsProvider, IRecipeItemsProvider
		{
			if (simulation is null)
				return false;

			return PassesBlock_CheckSimulation(thread, simulation);
		}

		private static bool PassesBlock_CheckRecipe<T>(T thread, Recipe recipe)
			where T : RefreshThread, IIngredientControlsProvider, IRecipeItemsProvider
		{
			IEnumerable<ItemInfo> ingredientsInfo = thread?.RecipeItems.storedIngredients.info.Value ?? storageItemInfo;
			List<ItemData> blockedIngredients = thread?.IngredientControls.blockStorageItems.Value ?? blockStorageItems;

			foreach (Item ingredient in recipe.requiredItem) {
				int stack = ingredient.stack;
				bool useRecipeGroup = false;

				foreach (ItemInfo item in ingredientsInfo) {
					if (!blockedIngredients.Contains(item) && RecipeGroupMatch(recipe, item.type, ingredient.type)) {
						stack -= item.stack;
						useRecipeGroup = true;

						if (stack <= 0)
							goto nextIngredient;
					}
				}

				if (!useRecipeGroup) {
					foreach (ItemInfo item in ingredientsInfo) {
						if (!blockedIngredients.Contains(item) && item.type == ingredient.type) {
							stack -= item.stack;

							if (stack <= 0)
								goto nextIngredient;
						}
					}
				}

				if (stack > 0)
					return false;

				nextIngredient: ;
			}

			return true;
		}

		private static bool PassesBlock_CheckSimulation<T>(T thread, CraftingSimulation simulation)
			where T : RefreshThread, IIngredientControlsProvider, IRecipeItemsProvider
		{
			IEnumerable<ItemInfo> ingredientsInfo = thread?.RecipeItems.storedIngredients.info.Value ?? storageItemInfo;
			List<ItemData> blockedIngredients = thread?.IngredientControls.blockStorageItems.Value ?? blockStorageItems;

			foreach (RequiredMaterialInfo material in simulation.RequiredMaterials) {
				int stack = material.Stack;

				foreach (int type in material.GetValidItems()) {
					foreach (ItemInfo item in ingredientsInfo) {
						if (!blockedIngredients.Contains(item) && item.type == type) {
							stack -= item.stack;

							if (stack <= 0)
								goto nextMaterial;
						}
					}
				}

				if (stack > 0)
					return false;

				nextMaterial: ;
			}

			return true;
		}
	}
}
