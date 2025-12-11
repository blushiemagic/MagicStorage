using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.Refreshing;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
	//	[ThreadStatic]
	//	internal static bool requestingAmountFromUI;

		internal static int AmountCraftable(Recipe recipe)
		{
			return AmountCraftable(NullThread, recipe);
		}

		internal static int AmountCraftable<T>(T thread, Recipe recipe)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, IRecipeSnapshotsProvider
		{
			int maxCrafts;

			NetHelper.Report(true, "Calculating maximum amount to craft for current recipe...");

			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
				NetHelper.Report(false, "Recipe had a recursion tree");

			//	using (FlagSwitch.ToggleTrue(ref requestingAmountFromUI))
				maxCrafts = recursiveRecipe.GetMaxCraftable(GetCurrentInventory(thread, cloneIfBlockEmpty: true));

				goto ReportAndReturn;
			}

			NetHelper.Report(false, "Recipe did not have a recursion tree or recursion was disabled");

			// Handle the old logic
			if (!IsAvailable(recipe)) {
				maxCrafts = 0;
				goto ReportAndReturn;
			}

			Dictionary<int, int> storageQuantity = thread?.ProcessedStorageItems.itemCounts.Value ?? itemCounts;
			bool hasCreativeUnit = thread?.IngredientControls.creativeUnitPresent.Value ?? allItemsAreInfinite;
			HashSet<int> infiniteItems = thread?.IngredientControls.infiniteItems.Value ?? isItemInfinite;

			if (hasCreativeUnit) {
				// No ingredients would be consumed
				maxCrafts = 9999;
				goto ReportAndReturn;
			}

			int resultItem = recipe.createItem.type;
			int resultStack = recipe.createItem.stack;

			int maxAllowedBatches = (int)(Utility.CeilingMultiple(9999u, (uint)resultStack) / (uint)resultStack);

			foreach (Item ingredient in recipe.requiredItem) {
				int stackConsumedPerCraft = ingredient.stack;

				if (ingredient.type == resultItem) {
					// Crafting the recipe would "undo" part or all of the ingredient consumption
					stackConsumedPerCraft -= resultStack;
				}

				if (stackConsumedPerCraft <= 0) {
					// Ingredient has a net zero or net gain after crafting the recipe
					continue;
				}

				if (!TryGetIngredientQuantity(recipe, storageQuantity, infiniteItems, ingredient.type, out int availableQuantity)) {
					// Ingredient has an infinite quantity
					continue;
				}

				// I don't quite know why this algorithm works, but it just does
				int possibleBatches = (availableQuantity - ingredient.stack) / stackConsumedPerCraft + 1;

				maxAllowedBatches = int.Min(maxAllowedBatches, possibleBatches);

				if (maxAllowedBatches <= 0) {
					maxCrafts = 0;
					goto ReportAndReturn;
				}
			}

			maxCrafts = int.Max(0, maxAllowedBatches * resultStack);

			ReportAndReturn:

			NetHelper.Report(false, $"Possible crafts = {maxCrafts}");

			return maxCrafts;
		}
	}
}
