using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class CraftingSimulation {
		private CraftResult simulationResult = CraftResult.Default;

		public IEnumerable<Recipe> UsedRecipes => simulationResult.usedRecipes.OrderBy(static r => r.recursionDepth).Select(static r => r.recipe).DistinctBy(static r => r, ReferenceEqualityComparer.Instance);
		public IReadOnlyList<RequiredMaterialInfo> RequiredMaterials => simulationResult.requiredMaterials;
		public IReadOnlyList<ItemInfo> ExcessResults => simulationResult.excessResults;
		public IEnumerable<int> RequiredTiles => simulationResult.requiredTiles;
		public IEnumerable<Condition> RequiredConditions => simulationResult.requiredConditions;

		public int AmountCrafted { get; private set; }

		public bool HasCondition(Condition condition) {
			return simulationResult.requiredConditions.Contains(condition);
		}

		public bool UsedRecipe(Recipe recipe) {
			return simulationResult.usedRecipes.Any(r => object.ReferenceEquals(r.recipe, recipe));
		}

		public void SimulateCrafts(RecursiveRecipe recipe, int amountToCraft, Dictionary<int, int> availableInventory) {
			simulationResult = CraftResult.Default;

			int sum = 0;
			int craftingTarget = Math.Min(amountToCraft, 9999);
			
			int iterations = 0;
			NetHelper.Report(true, "Requesting crafting simulation for max craftable...");

			while (sum < craftingTarget) {
				// Get the materials required, then "craft" it and continue checking until no more materials can be used
				CraftResult craftResult;
				using (FlagSwitch.Create(ref CraftingGUI.disableNetPrintingForIsAvailable, true))
					recipe.GetCraftingInformation(1, out craftResult, availableInventory);

				// Update the counts dictionary with the consumed materials
				bool notEnoughItems = false;
				foreach (RequiredMaterialInfo material in craftResult.requiredMaterials) {
					int stack = material.stack;
					if (stack > 0) {
						foreach (int item in material.GetValidItems()) {
							if (availableInventory.TryGetValue(item, out int quantity)) {
								if (quantity > stack) {
									// Reduce the available quantity
									availableInventory[item] = quantity - stack;
									stack = 0;
								} else {
									// Material was completely consumed
									availableInventory.Remove(item);
									stack -= quantity;
								}

								if (stack <= 0)
									break;
							}
						}

						if (stack > 0) {
							// Material requirement could not be satisfied
							notEnoughItems = true;
							break;
						}
					}
				}

				if (notEnoughItems)
					break;

				// Update the counts dictionary with the generated excess items
				foreach (ItemInfo info in craftResult.excessResults) {
					if (info.stack > 0)
						availableInventory.AddOrSumCount(info.type, info.stack);
				}

				simulationResult = simulationResult.CombineWith(craftResult);

				sum += recipe.original.createItem.stack;
				iterations++;
			}

			if (sum > craftingTarget)
				sum = craftingTarget;

			NetHelper.Report(true, $"Possible crafts = {sum}, Iterations: {iterations}");

			AmountCrafted = sum;
		}
	}
}
