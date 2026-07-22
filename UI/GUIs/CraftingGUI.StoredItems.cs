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
			var timing = GetRefreshTiming(thread);
			if (timing is not null) {
				timing.Measure(CraftingRefreshTimingPhase.StoredItems, () => RefreshStorageItemsInner(thread));
				return;
			}

			RefreshStorageItemsInner(thread);
		}

		private static void RefreshStorageItemsInner<T>(T thread)
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
					// Prefer the ledger-backed planner's concrete recipe path when it is available.
					int amountToCraft = thread.CraftingObject.craftAmountTarget.Value;
					var context = CreateCraftingSimulationContext(thread, amountToCraft);
					CraftingSimulation simulation = CreateCraftingSimulation(thread, recursiveRecipe, amountToCraft, GetCurrentInventory(thread), context);

					thread.RecipeSimulations.currentRecipeSimulation.Value = simulation;

					if (simulation.AmountCrafted > 0) {
						error = null;
						RefreshStorageItems_CheckRecursionRecipes(thread, selection, resultItemGroups, simulation.UsedRecipes);
					} else {
						// Show the information for ALL possible recipes in the tree
						RefreshStorageItems_CheckRecursionRecipes(thread, selection, resultItemGroups, selection.GetRecursiveRecipe().GetCraftingTree(cancellationToken: thread.cancellationToken).GetAllRecipes(thread.cancellationToken));
					}
				} else {
					int amountToCraft = thread.CraftingObject.craftAmountTarget.Value;
					var context = CreateCraftingSimulationContext(thread, amountToCraft);
					CraftingSimulation simulation = CreateCraftingSimulation(thread, recursiveRecipe, amountToCraft, GetCurrentInventory(thread), context);

					thread.RecipeSimulations.currentRecipeSimulation.Value = simulation;

					if (simulation.AmountCrafted > 0) {
						error = null;

						// Show the information for the materials that the exact simulation actually needs.
						RefreshStorageItems_CheckRequiredMaterials(thread, selection, resultItemGroups, simulation.RequiredMaterials);
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

			// Evaluate now so the material type index can be shared across all storage sources.
			List<Recipe> usedRecipes = recipes.ToList();
			HashSet<int> validIngredientTypes = BuildValidIngredientTypeSet(usedRecipes);

			thread.InitTaskSchedule(resultGroups.Count, "Populating Stored Ingredients");

			var handler = thread.RecipeItems;

			foreach (List<Item> itemsFromSource in resultGroups.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				foreach (Item item in itemsFromSource) {
					if (validIngredientTypes.Contains(item.type))
						handler.AddStoredIngredient(item);

					if (item.type == mainRecipe.createItem.type)
						handler.SetResultItem(item);
				}
			}
		}

		private static void RefreshStorageItems_CheckRequiredMaterials<T>(T thread, Recipe mainRecipe, List<List<Item>> resultGroups, IReadOnlyList<RequiredMaterialInfo> materials)
			where T : RefreshThread, IRecipeItemsProvider
		{
			NetHelper.Report(false, "Recipe had a recursive recipe, processing simulated required materials...");

			HashSet<int> validIngredientTypes = BuildValidIngredientTypeSet(materials);

			thread.InitTaskSchedule(resultGroups.Count, "Populating Stored Ingredients");

			var handler = thread.RecipeItems;

			foreach (List<Item> itemsFromSource in resultGroups.NotifyStepsTo(thread).WatchForCancellation(thread, 16)) {
				foreach (Item item in itemsFromSource) {
					if (validIngredientTypes.Contains(item.type))
						handler.AddStoredIngredient(item);

					if (item.type == mainRecipe.createItem.type)
						handler.SetResultItem(item);
				}
			}
		}

		private static HashSet<int> BuildValidIngredientTypeSet(List<Recipe> recipes) {
			HashSet<int> validTypes = new();

			foreach (Recipe recipe in recipes) {
				foreach (Item reqItem in recipe.requiredItem) {
					validTypes.Add(reqItem.type);

					foreach (int groupID in recipe.acceptedGroups) {
						RecipeGroup group = RecipeGroup.recipeGroups[groupID];
						if (!group.ContainsItem(reqItem.type))
							continue;

						foreach (int groupItemType in group.ValidItems)
							validTypes.Add(groupItemType);
					}
				}
			}

			return validTypes;
		}

		private static HashSet<int> BuildValidIngredientTypeSet(IReadOnlyList<RequiredMaterialInfo> materials) {
			HashSet<int> validTypes = new();

			foreach (RequiredMaterialInfo material in materials) {
				foreach (int type in material.GetValidItems())
					validTypes.Add(type);
			}

			return validTypes;
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
			// CHANGE: v0.7.1 - Allow result item to appear as an ingredient in duplication recipes
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
