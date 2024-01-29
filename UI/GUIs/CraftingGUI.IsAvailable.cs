using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
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
		public static bool IsCurrentRecipeFullyAvailable() => IsCurrentRecipeAvailable() && DoesCurrentRecipePassIngredientBlock();

		public static bool IsAvailable(Recipe recipe, bool checkRecursive = true) => IsAvailable(recipe, checkRecursive, recipe?.createItem.type ?? 0);

		private static bool IsAvailable(Recipe recipe, bool checkRecursive, int ignoreItem)
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
				if (currentlyThreading)
					available = IsAvailable_CheckRecursiveRecipe(recursiveRecipe, ignoreItem);
				else {
					ExecuteInCraftingGuiEnvironment(() => {
						available = IsAvailable_CheckRecursiveRecipe(recursiveRecipe, ignoreItem);
					});
				}
			} else
				available = IsAvailable_CheckNormalRecipe(recipe);

			if (!disableNetPrintingForIsAvailable)
				NetHelper.Report(true, $"Recipe {(available ? "was" : "was not")} available");

			return available;
		}

		private static bool IsAvailable_CheckRecursiveRecipe(RecursiveRecipe recipe, int ignoreItem) {
			var availableObjects = GetCurrentInventory(cloneIfBlockEmpty: true);
			if (ignoreItem > 0)
				availableObjects.RemoveIngredient(ignoreItem);

			using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI)) {
				CraftingSimulation simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recipe, 1, availableObjects);  // Recipe is available if at least one craft is possible
				return simulation.AmountCrafted > 0;
			}
		}

		private static bool IsAvailable_CheckNormalRecipe(Recipe recipe, int batches = 1) {
			if (recipe is null)
				return false;

			if (recipe.requiredTile.Any(tile => !adjTiles[tile]))
				return false;

			var itemCountsDictionary = GetItemCountsWithBlockedItemsRemoved();

			foreach (Item ingredient in recipe.requiredItem)
			{
				if (ingredient.stack * batches - IsAvailable_GetItemCount(recipe, ingredient.type, itemCountsDictionary) > 0)
					return false;
			}

			if (currentlyThreading)
				return MagicUI.activeThread.state is ThreadState state && state.recipeConditionsMetSnapshot[recipe.RecipeIndex];

			bool retValue = true;

			ExecuteInCraftingGuiEnvironment(() => {
				if (!RecipeLoader.RecipeAvailable(recipe))
					retValue = false;
			});

			return retValue;
		}

		private static int IsAvailable_GetItemCount(Recipe recipe, int type, Dictionary<int, int> itemCountsDictionary) {
			ClampedArithmetic count = 0;
			bool useRecipeGroup = false;
			foreach (var (item, quantity) in itemCountsDictionary) {
				if (RecipeGroupMatch(recipe, item, type)) {
					count += quantity;
					useRecipeGroup = true;
				}
			}

			if (!useRecipeGroup && itemCountsDictionary.TryGetValue(type, out int amount))
				count += amount;

			return count;
		}

		internal static bool PassesBlock(Recipe recipe)
		{
			if (recipe is null)
				return false;

			NetHelper.Report(true, "Checking if recipe passes \"blocked ingredients\" check...");

			bool success;
			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				var simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recursiveRecipe, craftAmountTarget, GetCurrentInventory(cloneIfBlockEmpty: true));

				success = PassesBlock_CheckSimulation(simulation);
			} else
				success = PassesBlock_CheckRecipe(recipe);

			NetHelper.Report(true, $"Recipe {(success ? "passed" : "failed")} the ingredients check");
			return success;
		}

		private static bool PassesBlock_CheckRecipe(Recipe recipe) {
			foreach (Item ingredient in recipe.requiredItem) {
				int stack = ingredient.stack;
				bool useRecipeGroup = false;

				foreach (ItemInfo item in storageItemInfo) {
					if (!blockStorageItems.Contains(new ItemData(item)) && RecipeGroupMatch(recipe, item.type, ingredient.type)) {
						stack -= item.stack;
						useRecipeGroup = true;

						if (stack <= 0)
							goto nextIngredient;
					}
				}

				if (!useRecipeGroup) {
					foreach (ItemInfo item in storageItemInfo) {
						if (!blockStorageItems.Contains(new ItemData(item)) && item.type == ingredient.type) {
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
			foreach (RequiredMaterialInfo material in simulation.RequiredMaterials) {
				int stack = material.Stack;

				foreach (int type in material.GetValidItems()) {
					foreach (ItemInfo item in storageItemInfo) {
						if (!blockStorageItems.Contains(new ItemData(item)) && item.type == type) {
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
