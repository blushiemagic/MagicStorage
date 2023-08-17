using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static readonly List<Item> storageItems = new();
		internal static readonly List<bool> storageItemsFromModules = new();
		private static List<ItemInfo> storageItemInfo;
		internal static readonly List<List<Item>> sourceItems = new();

		internal static Item result;

		private static void RefreshStorageItems(StorageGUI.ThreadContext thread = null)
		{
			NetHelper.Report(true, "Updating stored ingredients collection and result item...");

			storageItems.Clear();
			storageItemInfo = new();
			storageItemsFromModules.Clear();
			result = null;
			if (selectedRecipe is null) {
				thread?.InitAsCompleted("Populating stored ingredients");
				NetHelper.Report(true, "Failed.  No recipe is selected.");
				return;
			}

			int index = 0;
			bool hasItemFromStorage = false;
			if (!MagicStorageConfig.IsRecursionEnabled || !selectedRecipe.HasRecursiveRecipe() || GetCraftingSimulationForCurrentRecipe() is not CraftingSimulation { AmountCrafted: >0 } simulation) {
				NetHelper.Report(false, "Recursion was disabled or recipe did not have a recursive recipe");

				thread?.InitTaskSchedule(sourceItems.Count, "Populating stored ingredients");

				foreach (List<Item> itemsFromSource in sourceItems) {
					CheckStorageItemsForRecipe(selectedRecipe, itemsFromSource, null, checkResultItem: true, index, ref hasItemFromStorage);
					index++;

					thread?.CompleteOneTask();
				}
			} else {
				NetHelper.Report(false, "Recipe had a recursive recipe, processing recursion tree...");

				// Check each recipe in the tree
				IEnumerable<Recipe> recipes = simulation.UsedRecipes;

				// Evaluate now so the total task count can be used
				List<Recipe> usedRecipes = recipes.ToList();

				thread?.InitTaskSchedule(usedRecipes.Count * sourceItems.Count, "Populating stored ingredients");

				bool checkedHighestRecipe = false;
				List<bool[]> wasItemAdded = new List<bool[]>();
				foreach (Recipe recipe in usedRecipes) {
					index = 0;

					foreach (List<Item> itemsFromSource in sourceItems) {
						if (wasItemAdded.Count <= index)
							wasItemAdded.Add(new bool[itemsFromSource.Count]);

						// Only allow the "final recipe" (i.e. the first in the list) to affect the result item
						CheckStorageItemsForRecipe(recipe, itemsFromSource, wasItemAdded[index], checkResultItem: !checkedHighestRecipe, index, ref hasItemFromStorage);

						index++;

						thread?.CompleteOneTask();
					}

					checkedHighestRecipe = true;
				}
			}

			var resultItemList = CompactItemListWithModuleData(storageItems, storageItemsFromModules, out var moduleItemsList, thread);
			if (resultItemList.Count != storageItems.Count) {
				//Update the lists since items were compacted
				storageItems.Clear();
				storageItems.AddRange(resultItemList);
				storageItemInfo.Clear();
				storageItemInfo.AddRange(storageItems.Select(static i => new ItemInfo(i)));
				storageItemsFromModules.Clear();
				storageItemsFromModules.AddRange(moduleItemsList);
			}

			result ??= new Item(selectedRecipe.createItem.type, 0);

			NetHelper.Report(true, $"Success! Found {storageItems.Count} items and {(result.IsAir ? "no result items" : "a result item")}");
		}

		private static void CheckStorageItemsForRecipe(Recipe recipe, List<Item> itemsFromSource, bool[] wasItemAdded, bool checkResultItem, int index, ref bool hasItemFromStorage) {
			int addedIndex = 0;

			foreach (Item item in itemsFromSource) {
				if (item.type != selectedRecipe.createItem.type && wasItemAdded?[addedIndex] is not true) {
					foreach (Item reqItem in recipe.requiredItem) {
						if (item.type == reqItem.type || RecipeGroupMatch(recipe, item.type, reqItem.type)) {
							//Module items must refer to the original item instances
							Item clone = index >= numItemsWithoutSimulators ? item : item.Clone();
							storageItems.Add(clone);
							storageItemInfo.Add(new(clone));
							storageItemsFromModules.Add(index >= numItemsWithoutSimulators);

							if (wasItemAdded is not null)
								wasItemAdded[addedIndex] = true;
						}
					}
				}

				addedIndex++;

				if (checkResultItem && item.type == recipe.createItem.type) {
					Item source = itemsFromSource[0];

					if (index < numItemsWithoutSimulators) {
						result = source;
						hasItemFromStorage = true;
					} else if (!hasItemFromStorage)
						result = source;
				}
			}
		}

		private static List<Item> CompactItemListWithModuleData(List<Item> items, List<bool> moduleItems, out List<bool> moduleItemsResult, StorageGUI.ThreadContext thread = null) {
			List<Item> compacted = new();
			List<int> compactedSource = new();

			thread?.InitTaskSchedule(items.Count, "Aggregating stored ingredients (1/2)");

			for (int i = 0; i < items.Count; i++) {
				Item item = items[i];

				if (item.IsAir) {
					thread?.CompleteOneTask();
					continue;
				}

				bool fullyCompacted = false;
				for (int j = 0; j < compacted.Count; j++) {
					Item existing = compacted[j];

					if (ItemCombining.CanCombineItems(item, existing) && moduleItems[i] == moduleItems[compactedSource[j]] && !moduleItems[i]) {
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

				if (item.IsAir) {
					thread?.CompleteOneTask();
					continue;
				}

				if (!fullyCompacted) {
					compacted.Add(item);
					compactedSource.Add(i);
				}

				thread?.CompleteOneTask();
			}

			thread?.InitTaskSchedule(1, "Aggregating stored ingredients (2/2)");

			moduleItemsResult = compactedSource.Select(m => moduleItems[m]).ToList();

			thread?.CompleteOneTask();

			return compacted;
		}
	}
}
