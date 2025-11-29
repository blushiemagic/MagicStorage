using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class CraftingGUI {
		[ThreadStatic]
		internal static bool disableNetPrintingForIsAvailable;

		/// <summary>
		/// Returns <see langword="true"/> if the current recipe is available and passes the "blocked ingredients" filter
		/// </summary>
		[Obsolete("This method is functionally identical to " + nameof(IsCurrentRecipeAvailable) + "().", error: true)]
		public static bool IsCurrentRecipeFullyAvailable() => IsCurrentRecipeAvailable();

		public static bool IsAvailable(Recipe recipe, bool checkRecursive = true)
		{
			if (recipe is null)
				return false;

			if (!disableNetPrintingForIsAvailable) {
				NetHelper.Report(true, "Checking if recipe is available...");

				if (checkRecursive && MagicStorageConfig.IsRecursionEnabled)
					NetHelper.Report(false, "Calculating recursion tree for recipe...");
			}

			bool available = false;
			if (checkRecursive && MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				if (MagicUI.CurrentlyRefreshing)
					available = IsAvailable_CheckRecursiveRecipe(recursiveRecipe);
				else
					available = ExecuteInCraftingGuiEnvironment(recursiveRecipe, IsAvailable_CheckRecursiveRecipe);
			} else
				available = IsAvailable_CheckNormalRecipe(recipe);

			// Cache the availability of the selected recipe here, if applicable
			Recipe selected = MagicUI.HasActiveThread(out CommonCraftingThread thread) ? thread.selectedRecipe : selectedRecipe;

			if (object.ReferenceEquals(recipe, selected)) {
				recentRecipeAvailable = recipe;
				currentRecipeIsAvailable = available;
			}

			if (!disableNetPrintingForIsAvailable)
				NetHelper.Report(true, $"Recipe {(available ? "was" : "was not")} available");

			return available;
		}

		// CHANGE: v0.7.0.12 - No longer has an "int ignoreItem" parameter that was used to ignore the recipe's result item
		private static bool IsAvailable_CheckRecursiveRecipe(RecursiveRecipe recipe) {
			var availableObjects = GetCurrentInventory(cloneIfBlockEmpty: true);

			using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI)) {
				CraftingSimulation simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recipe, 1, availableObjects);  // Recipe is available if at least one craft is possible
				return simulation.AmountCrafted > 0;
			}
		}

		private static bool IsAvailable_CheckNormalRecipe(Recipe recipe) {
			if (recipe is null)
				return false;

			if (recipe.requiredTile.Any(tile => !adjTiles[tile]))
				return false;

			HashSet<int> infiniteItems;
			if (MagicUI.HasActiveThread(out CraftingRefreshThread thread)) {
				if (thread.creativeUnitPresent)
					goto SkipIngredientChecks;

				infiniteItems = thread.infiniteItems;
			} else {
				if (allItemsAreInfinite)
					goto SkipIngredientChecks;

				infiniteItems = isItemInfinite;
			}

			var itemCountsDictionary = GetItemCountsWithBlockedItemsRemoved();

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

			// TODO: find all references for blockStorageItems

			SkipIngredientChecks:

			if (thread is not null)
				return thread.recipeConditionsMetSnapshot[recipe.RecipeIndex];

			return ExecuteInCraftingGuiEnvironment(recipe, RecipeLoader.RecipeAvailable);
		}

		internal static bool PassesBlock(Recipe recipe)
		{
			if (recipe is null)
				return false;

			NetHelper.Report(true, "Checking if recipe passes \"blocked ingredients\" check...");

			bool success;
			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				int amountToCraft = MagicUI.HasActiveThread(out CommonCraftingThread thread) ? thread.craftAmountTarget : craftAmountTarget;

				var simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recursiveRecipe, amountToCraft, GetCurrentInventory(cloneIfBlockEmpty: true));

				success = PassesBlock_CheckSimulation(simulation);
			} else
				success = PassesBlock_CheckRecipe(recipe);

			NetHelper.Report(true, $"Recipe {(success ? "passed" : "failed")} the ingredients check");
			return success;
		}

		private static bool PassesBlock_CheckRecipe(Recipe recipe) {
			IEnumerable<ItemInfo> ingredientsInfo;
			List<ItemData> blockedIngredients;

			if (MagicUI.HasActiveThread(out CommonCraftingThread thread)) {
				ingredientsInfo = thread.recipeItemsHandler.GetIngredientsInfo();
				blockedIngredients = thread.blockStorageItems;
			} else {
				ingredientsInfo = storageItemInfo;
				blockedIngredients = blockStorageItems;
			}

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

		private static bool PassesBlock_CheckSimulation(CraftingSimulation simulation) {
			IEnumerable<ItemInfo> ingredientsInfo;
			List<ItemData> blockedIngredients;

			if (MagicUI.HasActiveThread(out CommonCraftingThread thread)) {
				ingredientsInfo = thread.recipeItemsHandler.GetIngredientsInfo();
				blockedIngredients = thread.blockStorageItems;
			} else {
				ingredientsInfo = storageItemInfo;
				blockedIngredients = blockStorageItems;
			}

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
