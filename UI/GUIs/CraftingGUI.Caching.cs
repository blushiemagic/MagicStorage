using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.Refreshing;
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

		internal static void SetRecipeAndCraftingCaches<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSnapshotsProvider
		{
			if (thread.CraftingObject.selection.Value is not Recipe recipe) {
				thread.CraftingObject.craftAmountTarget.Value = 1;
				return;
			}
			
			selectedRecipe = recipe;

			thread.InitTaskSchedule(
				totalTasks: MagicStorageConfig.IsRecursionEnabled && recipe.HasRecursiveRecipe() ? 4 : 3,
				taskName: "Populating Caches"
			);

			int maxCraftable;
			recentRecipeAmountCraftable = recipe;
			amountCraftableForCurrentRecipe = maxCraftable = AmountCraftable(thread, recipe);

			thread.CraftingObject.craftAmountTarget.Value = Utils.Clamp(thread.CraftingObject.craftAmountTarget.Value, 1, maxCraftable);

			thread.CompleteOne();

			recentRecipeAvailable = recipe;
			currentRecipeIsAvailable = IsAvailable(thread, recipe);

			thread.CompleteOne();

			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out var recursiveRecipe)) {
				recentRecipeSimulation = recipe;
				CraftingSimulation simulation = new CraftingSimulation();
				simulation.SimulateCrafts(recursiveRecipe, thread.CraftingObject.craftAmountTarget.Value, GetCurrentInventory(thread, cloneIfBlockEmpty: true));
				simulatedCraftForCurrentRecipe = simulation;

				thread.CompleteOne();
			}

			// Obsolete, but still needs to be processed
			recentRecipeBlock = recipe;
			currentRecipePassesBlock = PassesBlock(thread, recipe);

			thread.CompleteOne();
		}

		public static int AmountCraftableForCurrentRecipe() {
			if (MagicUI.CurrentlyRefreshing || selectedRecipe is null)
				return 0;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeAmountCraftable, selectedRecipe) && amountCraftableForCurrentRecipe is { } amount)
				return amount;

			// Calculate the value
			recentRecipeAmountCraftable = selectedRecipe;
			amountCraftableForCurrentRecipe = amount = AmountCraftable(selectedRecipe);
			return amount;
		}

		public static bool IsCurrentRecipeAvailable() {
			if (MagicUI.CurrentlyRefreshing || selectedRecipe is null)
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
			if (MagicUI.CurrentlyRefreshing || selectedRecipe is null)
				return false;  // Delay logic until threading stops

			if (object.ReferenceEquals(recentRecipeBlock, selectedRecipe) && currentRecipePassesBlock is { } available)
				return available;

			// Calculate the value
			recentRecipeBlock = selectedRecipe;
			currentRecipePassesBlock = available = PassesBlock(selectedRecipe);
			return available;
		}

		public static CraftingSimulation GetCraftingSimulationForCurrentRecipe() {
			if (MagicUI.CurrentlyRefreshing || selectedRecipe is null)
				return new CraftingSimulation();

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
