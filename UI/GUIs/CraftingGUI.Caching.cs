using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using System;
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

		public static void ResetRecentRecipeCache() {
			recentRecipeAvailable = null;
			currentRecipeIsAvailable = null;
			recentRecipeBlock = null;
			currentRecipePassesBlock = null;
			recentRecipeAmountCraftable = null;
			amountCraftableForCurrentRecipe = null;
			recentRecipeSimulation = null;
			simulatedCraftForCurrentRecipe = null;
		}

		internal static void SetRecipeAndCraftingCaches(CommonCraftingThread thread) {
			if (thread.selectedRecipe is not Recipe recipe) {
				thread.craftAmountTarget = 1;
				return;
			}
			
			selectedRecipe = recipe;

			thread.InitTaskSchedule(
				totalTasks: MagicStorageConfig.IsRecursionEnabled && recipe.HasRecursiveRecipe() ? 4 : 3,
				taskName: "Populating Caches"
			);

			int maxCraftable;
			recentRecipeAmountCraftable = recipe;
			amountCraftableForCurrentRecipe = maxCraftable = AmountCraftable(recipe);

			thread.craftAmountTarget = Utils.Clamp(thread.craftAmountTarget, 1, maxCraftable);

			thread.CompleteOne();

			// IsAvailable() will automatically populate this if the selected recipe was in the recipe list
			// When it isn't, this result should still be cached
			if (recentRecipeAvailable is null) {
				recentRecipeAvailable = recipe;
				currentRecipeIsAvailable = IsAvailable(recipe);
			}

			thread.CompleteOne();

			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out var recursiveRecipe)) {
				recentRecipeSimulation = recipe;
				CraftingSimulation simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recursiveRecipe, thread.craftAmountTarget, GetCurrentInventory(cloneIfBlockEmpty: true));
				simulatedCraftForCurrentRecipe = simulation;

				thread.CompleteOne();
			}

			// Obsolete, but still needs to be processed
			recentRecipeBlock = recipe;
			currentRecipePassesBlock = PassesBlock(recipe);

			thread.CompleteOne();
		}

		public static int AmountCraftableForCurrentRecipe() {
			if (MagicUI.CurrentlyRefreshing)
				return 0;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeAmountCraftable, selectedRecipe) && amountCraftableForCurrentRecipe is { } amount)
				return amount;

			// Calculate the value
			recentRecipeAmountCraftable = selectedRecipe;
			amountCraftableForCurrentRecipe = amount = AmountCraftable(selectedRecipe);
			return amount;
		}

		public static bool IsCurrentRecipeAvailable() {
			if (MagicUI.CurrentlyRefreshing)
				return false;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeAvailable, selectedRecipe) && currentRecipeIsAvailable is { } available)
				return available;

			// Calculate the value
			recentRecipeAvailable = selectedRecipe;
			currentRecipeIsAvailable = available = IsAvailable(selectedRecipe);
			return available;
		}

		[Obsolete("The blocked ingredients check is now part of the recipe availability checks.", error: true)]
		public static bool DoesCurrentRecipePassIngredientBlock() {
			if (MagicUI.CurrentlyRefreshing)
				return false;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeBlock, selectedRecipe) && currentRecipePassesBlock is { } available)
				return available;

			// Calculate the value
			recentRecipeBlock = selectedRecipe;
			currentRecipePassesBlock = available = PassesBlock(selectedRecipe);
			return available;
		}

		public static CraftingSimulation GetCraftingSimulationForCurrentRecipe() {
			if (object.ReferenceEquals(recentRecipeSimulation, selectedRecipe) && simulatedCraftForCurrentRecipe is not null)
				return simulatedCraftForCurrentRecipe;

			if (!selectedRecipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe))
				return new CraftingSimulation();

			// Calculate the value
			recentRecipeSimulation = selectedRecipe;
			CraftingSimulation simulation = new CraftingSimulation();
			simulation.SimulateCrafts(recursiveRecipe, craftAmountTarget, GetCurrentInventory(cloneIfBlockEmpty: true));
			simulatedCraftForCurrentRecipe = simulation;
			return simulation;
		}
	}
}
