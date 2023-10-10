using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common;
using Terraria;
using System.Linq;
using System;

namespace MagicStorage {
	partial class CraftingGUI {
		[ThreadStatic]
		internal static bool requestingAmountFromUI;

		// Calculates how many times a recipe can be crafted using available items
		internal static int AmountCraftable(Recipe recipe)
		{
			NetHelper.Report(true, "Calculating maximum amount to craft for current recipe...");

			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				NetHelper.Report(false, "Recipe had a recursion tree");

				using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI))
					return recursiveRecipe.GetMaxCraftable(GetCurrentInventory(cloneIfBlockEmpty: true));
			}

			NetHelper.Report(false, "Recipe did not hae a recursion tree or recursion was disabled");

			// Handle the old logic
			if (!IsAvailable(recipe))
				return 0;

			// Local capturing
			Recipe r = recipe;

			int GetMaxCraftsAmount(Item requiredItem) {
				ClampedArithmetic total = 0;
				foreach (Item inventoryItem in items) {
					if (inventoryItem.type == requiredItem.type || RecipeGroupMatch(r, inventoryItem.type, requiredItem.type))
						total += inventoryItem.stack;
				}

				int craftable = total / requiredItem.stack;
				return craftable;
			}

			int maxCrafts = recipe.requiredItem.Select(GetMaxCraftsAmount).Prepend(9999).Min() * recipe.createItem.stack;

			if ((uint)maxCrafts > 9999)
				maxCrafts = 9999;

			NetHelper.Report(false, $"Possible crafts = {maxCrafts}");

			return maxCrafts;
		}
	}
}
