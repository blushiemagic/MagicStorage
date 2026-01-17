using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;

namespace MagicStorage {
	partial class CraftingGUI {
		[Obsolete("Replaced by RefreshThread interfaces", error: true)]
		public class StoredItemAggregator {
			public List<Item> items;
			public List<bool> itemsFromModules;
			public List<ItemInfo> itemInfo;

			public StoredItemAggregator(List<Item> items, List<bool> itemsFromModules, List<ItemInfo> itemInfo) {
				this.items = items;
				this.itemsFromModules = itemsFromModules;
				this.itemInfo = itemInfo;
			}
		}

		internal static readonly List<Item> storageItems = new();
		internal static readonly List<ItemInfo> storageItemInfo = new();

		internal static bool showAllPossibleIngredients;
		internal static string lastKnownRecursionErrorForStoredItems;
		internal static string lastKnownRecursionErrorForObjects;

		internal static Item result;

		private static void RefreshStorageItems<T>(T thread)
			where T : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, IRecipeItemsProvider, IRecipeSimulationsProvider, IRecipeSnapshotsProvider
		{
			NetHelper.Report(true, "Updating stored ingredients collection and result item...");

			var selection = thread.CraftingObject.selection.Value;

			if (selection is null) {
				thread.InitAsCompleted("Populating Stored Ingredients");
				NetHelper.Report(true, "Failed.  No recipe is selected.");
				return;
			}

			ref string error = ref thread.storedItemsError;

			var resultItemGroups = thread.ProcessedStorageItems.resultItemGroups.Value;

			if (!MagicStorageConfig.IsRecursionEnabled || !selection.TryGetRecursiveRecipe(out var recursiveRecipe)) {
				// Show the information for the recipe that was selected
				RefreshStorageItems_CheckNormalRecipe(thread, selection, resultItemGroups);

				if (MagicStorageConfig.IsRecursionEnabled)
					error = Language.GetTextValue("Mods.MagicStorage.CraftingGUI.RecursionErrors.NoRecipe");
			} else {
				if (thread.IngredientControls.showAllPossibleIngredients.Value) {
					// Show the information for ALL possible recipes in the tree
					RefreshStorageItems_CheckRecursionRecipes(thread, selection, resultItemGroups, selection.GetRecursiveRecipe().GetCraftingTree().GetAllRecipes());
				} else {
					CraftingSimulation simulation = new();
					simulation.SimulateCrafts(recursiveRecipe, thread.CraftingObject.craftAmountTarget.Value, GetCurrentInventory(thread, cloneIfBlockEmpty: true));

					thread.RecipeSimulations.currentRecipeSimulation.Value = simulation;

					if (simulation.AmountCrafted > 0) {
						// Show the information for the recipes that were used by the simulation
						RefreshStorageItems_CheckRecursionRecipes(thread, selection, resultItemGroups, simulation.UsedRecipes);
					} else {
						// Show the information for the highest recipe in the tree, since the simulation failed
						RefreshStorageItems_CheckNormalRecipe(thread, selection, resultItemGroups);

						error = Language.GetTextValue("Mods.MagicStorage.CraftingGUI.RecursionErrors.NoIngredients");
					}
				}
			}

			thread.RecipeItems.CompactCollections(thread);

			NetHelper.Report(true, $"Success! Found {thread.RecipeItems.GetItemCountsReport()}");
		}

		private static void RefreshStorageItems_CheckNormalRecipe<T>(T thread, Recipe recipe, List<List<Item>> resultItemGroups)
			where T : RefreshThread, IRecipeItemsProvider
		{
			NetHelper.Report(false, "Recursion was disabled or recipe did not have a recursive recipe");

			thread.InitTaskSchedule(resultItemGroups.Count, "Populating Stored Ingredients");

			var handler = thread.RecipeItems;

			foreach (var items in resultItemGroups.NotifyStepsTo(thread).WatchForCancellation(thread, 16))
				CheckStorageItemsForRecipe(recipe, handler, items, null, checkResultItem: true);
		}

		private static void RefreshStorageItems_CheckRecursionRecipes<T>(T thread, Recipe mainRecipe, List<List<Item>> resultGroups, IEnumerable<Recipe> recipes)
			where T : RefreshThread, IRecipeItemsProvider
		{
			NetHelper.Report(false, "Recipe had a recursive recipe, processing recursion tree...");

			// Check each recipe in the tree
			// Evaluate now so the total task count can be used
			List<Recipe> usedRecipes = recipes.ToList();

			thread.InitTaskSchedule(usedRecipes.Count * resultGroups.Count, "Populating Stored Ingredients");

			var handler = thread.RecipeItems;

			int index;
			List<bool[]> wasItemAdded = [.. resultGroups.Select(list => new bool[list.Count])];
			foreach (Recipe recipe in usedRecipes.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				index = 0;

				foreach (List<Item> itemsFromSource in resultGroups.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
					// Only allow the "final recipe" (i.e. the first in the list) to affect the result item
					CheckStorageItemsForRecipe(recipe, handler, itemsFromSource, wasItemAdded[index++], checkResultItem: object.ReferenceEquals(recipe, mainRecipe));
				}
			}
		}

		private static void CheckStorageItemsForRecipe(Recipe recipe, RecipeItems handler, List<Item> itemsFromSource, bool[] wasItemAdded, bool checkResultItem) {
			int addedIndex = 0;

			foreach (Item item in itemsFromSource) {
				if (wasItemAdded is not null) {
					if (!wasItemAdded[addedIndex] && IsItemValidForRecipe(item, recipe))
						wasItemAdded[addedIndex] = CheckItemFromSource(handler, item, recipe, IsItemValidForRecipe);

					addedIndex++;
				} else
					CheckItemFromSource(handler, item, recipe, IsItemValidForRecipe);

				if (checkResultItem && item.type == recipe.createItem.type)
					handler.SetResultItem(item);
			}
		}

		private static bool IsItemValidForRecipe(Item item, Recipe recipe) {
			// CHANGE: v0.7.0.12 - Allow result item to appear as an ingredient in duplication recipes
			/*
			if (item.type == selectedRecipe.createItem.type)
				return false;
			*/

			foreach (Item reqItem in recipe.requiredItem) {
				if (item.type == reqItem.type || RecipeGroupMatch(recipe, item.type, reqItem.type))
					return true;
			}

			return false;
		}

		internal static bool CheckItemFromSource(RecipeItems handler, Item item, Func<Item, bool> isItemValid) {
			if (!isItemValid(item))
				return false;

			handler.AddStoredIngredient(item);

			return true;
		}

		internal static bool CheckItemFromSource<T>(RecipeItems handler, Item item, T state, Func<Item, T, bool> isItemValid) {
			if (!isItemValid(item, state))
				return false;

			handler.AddStoredIngredient(item);

			return true;
		}

		internal static void CompactItemList(RefreshThread thread, ItemInfoListProvider provider, Func<Item, bool> isModuleItem, string collectionClassification) {
			List<Item> compacted = new();

			thread.InitTaskSchedule(provider.items.Count, $"Aggregating {collectionClassification}");

			foreach (Item item in provider.items.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				if (item.IsAir)
					continue;

				bool fullyCompacted = false;
				if (isModuleItem(item))
					goto CheckCompactInsertion;

				for (int j = 0; j < compacted.Count; j++) {
					Item existing = compacted[j];

					if (StorageAggregator.CanCombineItems(item, existing)) {
						if (existing.stack + item.stack <= existing.maxStack) {
							existing.stack += item.stack;
							item.stack = 0;
							fullyCompacted = true;
						} else {
							int diff = existing.maxStack - existing.stack;

							Utility.CallOnStackHooks(existing, item, diff);

							existing.stack = existing.maxStack;
							item.stack -= diff;
						}

						break;
					}
				}

				CheckCompactInsertion:

				if (!item.IsAir && !fullyCompacted)
					compacted.Add(item);
			}

			if (compacted.Count != provider.items.Count) {
				provider.items.Clear();
				provider.items.AddRange(compacted);
				provider.info.Clear();
				provider.info.AddRange(compacted.Select(ItemInfo.FromItem));
			}
		}
	}
}
