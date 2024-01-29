using MagicStorage.Common.Systems.RecurrentRecipes;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace MagicStorage {
	partial class CraftingGUI {
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
		internal static readonly List<bool> storageItemsFromModules = new();
		internal static readonly List<ItemInfo> storageItemInfo = new();
		internal static readonly List<List<Item>> sourceItems = new();

		internal static bool showAllPossibleIngredients;
		internal static string lastKnownRecursionErrorForStoredItems;
		internal static string lastKnownRecursionErrorForObjects;

		internal static Item result;

		private static void RefreshStorageItems(StorageGUI.ThreadContext thread = null)
		{
			NetHelper.Report(true, "Updating stored ingredients collection and result item...");

			storageItems.Clear();
			storageItemInfo.Clear();
			storageItemsFromModules.Clear();
			result = null;
			if (selectedRecipe is null) {
				thread?.InitAsCompleted("Populating stored ingredients");
				NetHelper.Report(true, "Failed.  No recipe is selected.");
				return;
			}

			ref string error = ref lastKnownRecursionErrorForStoredItems;
			if (thread is not null) {
				if (thread.state is not ThreadState state) {
					thread?.InitAsCompleted("Populating stored ingredients");
					NetHelper.Report(true, "Failed.  Thread state is not valid.");
					return;
				}

				error = ref state.recursionFailReason;
			}

			error = null;

			if (!MagicStorageConfig.IsRecursionEnabled || !selectedRecipe.HasRecursiveRecipe() || GetCraftingSimulationForCurrentRecipe() is not CraftingSimulation simulation) {
				// Show the information for the recipe that was selected
				RefreshStorageItems_CheckNormalRecipe(thread);

				if (MagicStorageConfig.IsRecursionEnabled)
					error = Language.GetTextValue("Mods.MagicStorage.CraftingGUI.RecursionErrors.NoRecipe");
			} else {
				if (showAllPossibleIngredients) {
					// Show the information for ALL possible recipes in the tree
					RefreshStorageItems_CheckRecursionRecipes(thread, selectedRecipe.GetRecursiveRecipe().GetCraftingTree().GetAllRecipes());
				} else if (simulation.AmountCrafted > 0) {
					// Show the information for the recipes that were used by the simulation
					RefreshStorageItems_CheckRecursionRecipes(thread, simulation.UsedRecipes);
				} else {
					// Show the information for the highest recipe in the tree, since the simulation failed
					RefreshStorageItems_CheckNormalRecipe(thread);

					error = Language.GetTextValue("Mods.MagicStorage.CraftingGUI.RecursionErrors.NoIngredients");
				}
			}

			AttemptListCompact();

			result ??= new Item(selectedRecipe.createItem.type, 0);

			NetHelper.Report(true, $"Success! Found {storageItems.Count} items and {(result.IsAir ? "no result items" : "a result item")}");
		}

		private static void RefreshStorageItems_CheckNormalRecipe(StorageGUI.ThreadContext thread) {
			NetHelper.Report(false, "Recursion was disabled or recipe did not have a recursive recipe");

			thread?.InitTaskSchedule(sourceItems.Count, "Populating stored ingredients");

			int index = 0;
			bool hasItemFromStorage = false;
			foreach (List<Item> itemsFromSource in sourceItems) {
				CheckStorageItemsForRecipe(selectedRecipe, itemsFromSource, null, checkResultItem: true, index, ref hasItemFromStorage);
				index++;

				thread?.CompleteOneTask();
			}
		}

		private static void RefreshStorageItems_CheckRecursionRecipes(StorageGUI.ThreadContext thread, IEnumerable<Recipe> recipes) {
			NetHelper.Report(false, "Recipe had a recursive recipe, processing recursion tree...");

			// Check each recipe in the tree
			// Evaluate now so the total task count can be used
			List<Recipe> usedRecipes = recipes.ToList();

			thread?.InitTaskSchedule(usedRecipes.Count * sourceItems.Count, "Populating stored ingredients");

			int index;
			bool hasItemFromStorage = false;
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

		private static void CheckStorageItemsForRecipe(Recipe recipe, List<Item> itemsFromSource, bool[] wasItemAdded, bool checkResultItem, int index, ref bool hasItemFromStorage) {
			int addedIndex = 0;

			foreach (Item item in itemsFromSource) {
				bool b = false;
				ref bool added = ref wasItemAdded is null ? ref b : ref wasItemAdded[addedIndex];
				CheckItemFromSource(null, item, recipe, index, ref added, IsItemValidForRecipe);

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

		private static bool IsItemValidForRecipe(Item item, Recipe recipe) {
			if (item.type == selectedRecipe.createItem.type)
				return false;

			foreach (Item reqItem in recipe.requiredItem) {
				if (item.type == reqItem.type || RecipeGroupMatch(recipe, item.type, reqItem.type))
					return true;
			}

			return false;
		}

		internal static void CheckItemFromSource(StoredItemAggregator aggregator, Item item, int itemIndexInSource, ref bool wasItemAdded, Func<Item, bool> isItemValid) {
			if (wasItemAdded || !isItemValid(item))
				return;

			AddItemToAggregateCollections(aggregator, item, itemIndexInSource);

			wasItemAdded = true;
		}

		internal static void CheckItemFromSource<T>(StoredItemAggregator aggregator, Item item, T state, int itemIndexInSource, ref bool wasItemAdded, Func<Item, T, bool> isItemValid) {
			if (wasItemAdded || !isItemValid(item, state))
				return;

			AddItemToAggregateCollections(aggregator, item, itemIndexInSource);

			wasItemAdded = true;
		}

		private static void AddItemToAggregateCollections(StoredItemAggregator aggregator, Item item, int itemIndexInSource) {
			List<Item> items;
			List<bool> itemsFromModules;
			List<ItemInfo> itemInfo;

			if (aggregator is null) {
				items = storageItems;
				itemsFromModules = storageItemsFromModules;
				itemInfo = storageItemInfo;
			} else {
				items = aggregator.items;
				itemsFromModules = aggregator.itemsFromModules;
				itemInfo = aggregator.itemInfo;
			}

			// Module items must refer to the original item instances
			Item clone = itemIndexInSource >= numItemsWithoutSimulators ? item : item.Clone();
			items.Add(clone);
			itemInfo.Add(new(clone));
			itemsFromModules.Add(itemIndexInSource >= numItemsWithoutSimulators);
		}

		internal static void AttemptListCompact(StorageGUI.ThreadContext thread = null) {
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
