using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.Refreshing;
using System;
using System.Collections.Generic;
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

		// CHANGE: v0.7.0.12 - No longer has an "int ignoreItem" parameter that was used to ignore the recipe's result item
		private static bool IsAvailable_CheckRecursiveRecipe<T>(T thread, Recipe recipe, RecursiveRecipe recursiveRecipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, ICraftingObjectProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			var lookup = thread?.RecipeSimulations.recipeToAvailableSimulation.Value ?? recursionRecipeToAvailableSimulationLookup;
			if (lookup.TryGetValue(recipe, out var simulation))
				return simulation.AmountCrafted > 0;

			var availableObjects = GetCurrentInventory(thread, cloneIfBlockEmpty: true);

		//	using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI)) {
			lookup.AddOrUpdate(recipe, simulation = new());
			simulation.SimulateCrafts(recursiveRecipe, 1, availableObjects);  // Recipe is available if at least one craft is possible
			return simulation.AmountCrafted > 0;
		//	}
		}

		private static bool IsAvailable_CheckNormalRecipe<T>(T thread, Recipe recipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, IRecipeSnapshotsProvider
		{
			if (recipe is null)
				return false;

			// CHANGE: v0.7.0.12 - Condition checks are moved first to better optimize RecipeWatchTarget
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
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, IRecipeSnapshotsProvider, IRecipeItemsProvider
		{
			if (recipe is null)
				return false;

		//	NetHelper.Report(true, "Checking if recipe passes \"blocked ingredients\" check...");

			bool success;
			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				int amountToCraft = thread?.CraftingObject.craftAmountTarget.Value ?? craftAmountTarget;

				var simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recursiveRecipe, amountToCraft, GetCurrentInventory(thread, cloneIfBlockEmpty: true));

				success = PassesBlock_CheckSimulation(thread, simulation);
			} else
				success = PassesBlock_CheckRecipe(thread, recipe);

		//	NetHelper.Report(true, $"Recipe {(success ? "passed" : "failed")} the ingredients check");
			return success;
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
