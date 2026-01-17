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
			amountCraftableForCurrentRecipe = maxCraftable = AmountCraftable(thread, recipe);

			thread.CompleteOne();

			targetProvider.Value = Utils.Clamp(targetProvider.Value, 1, maxCraftable);

			currentRecipeIsAvailable = IsAvailable(thread, recipe);

			thread.CompleteOne();

			if (MagicStorageConfig.IsRecursionEnabled && recipe.TryGetRecursiveRecipe(out var recursiveRecipe)) {
				var simulationProvider = thread.RecipeSimulations.currentRecipeSimulation;

				if (simulationProvider.Value is not { } simulation) {
					simulationProvider.Value = simulation = new();
					simulation.SimulateCrafts(recursiveRecipe, targetProvider.Value, GetCurrentInventory(thread, cloneIfBlockEmpty: true));
				}

				simulatedCraftForCurrentRecipe = simulation;
			} else
				simulatedCraftForCurrentRecipe = new CraftingSimulation();

			thread.CompleteOne();

			// Obsolete, but still needs to be processed
			recentRecipeBlock = recipe;
			currentRecipePassesBlock = PassesBlock(thread, recipe);

			thread.CompleteOne();

			thread.CraftingObject.CopyToStaticFields();
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

			recentRecipeSimulation = selectedRecipe;

			if (!MagicStorageConfig.IsRecursionEnabled || !selectedRecipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe))
				return simulatedCraftForCurrentRecipe = new CraftingSimulation();

			// Calculate the value
			CraftingSimulation simulation = new CraftingSimulation();
			simulation.SimulateCrafts(recursiveRecipe, craftAmountTarget, GetCurrentInventory(cloneIfBlockEmpty: true));
			simulatedCraftForCurrentRecipe = simulation;
			return simulation;
		}
	}
}
