using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public readonly struct ItemInfo {
		public readonly int type;
		public readonly int stack;
		public readonly int prefix;  // Used when converting to ItemData

		public ItemInfo(int type, int stack, int prefix = 0) {
			this.type = type;
			this.stack = stack;
			this.prefix = prefix;
		}

		public ItemInfo(Item item) : this(item.type, item.stack, item.prefix) { }
	}

	public sealed class RecursiveRecipe {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				recipeToRecursiveRecipe.Clear();
			}
		}

		/// <summary>
		/// The base recipe object
		/// </summary>
		public readonly Recipe original;

		/// <summary>
		/// The tree representing the recipes used to recursive craft this recipe
		/// </summary>
		public readonly RecursionTree tree;

		internal static readonly ConditionalWeakTable<Recipe, RecursiveRecipe> recipeToRecursiveRecipe = new();

		public RecursiveRecipe(Recipe recipe) {
			original = recipe;
			tree = new RecursionTree(recipe);
		}

		public static void RecalculateAllRecursiveRecipes() {
			foreach (var (_, recursive) in recipeToRecursiveRecipe) {
				recursive.tree.Reset();
				recursive.tree.CalculateTree();
			}

			NodePool.ClearNodes();
		}

		/// <summary>
		/// Returns an enumeration of every recipe used in this recursion tree
		/// </summary>
		public IEnumerable<Recipe> GetAllRecipes() {
			HashSet<int> checkedRecipes = new();
			Queue<RecursiveRecipe> recipeQueue = new();
			recipeQueue.Enqueue(this);
			Queue<int> depths = new();
			depths.Enqueue(0);

			while (recipeQueue.TryDequeue(out RecursiveRecipe recipe)) {
				Node root = recipe.tree.Root;
				int depth = depths.Dequeue();

				if (!MagicStorageConfig.IsRecursionInfinite && depth >= MagicStorageConfig.RecipeRecursionDepth)
					continue;

				if (!checkedRecipes.Add(root.poolIndex))
					continue;

				yield return recipe.original;

				foreach (var ingredient in root.info.ingredientTrees) {
					if (ingredient.RecipeCount == 0 || !ingredient.SelectedRecipe.TryGetRecursiveRecipe(out RecursiveRecipe ingredientRecipe))
						continue;

					recipeQueue.Enqueue(ingredientRecipe);
					depths.Enqueue(depth + 1);
				}
			}
		}

		/// <summary>
		/// Returns a tree representing the recipes this recursive recipe will use and their expected crafted quantities
		/// </summary>
		/// <param name="amountToCraft">How many items are expected to be crafted.  Defaults to 1</param>
		/// <param name="availableInventory">An optional dictionary indicating which item types are available and their quantities</param>
		public OrderedRecipeTree GetCraftingTree(int amountToCraft = 1, Dictionary<int, int> availableInventory = null) {
			int batchSize = original.createItem.stack;
			int batches = (int)Math.Ceiling(amountToCraft / (double)batchSize);

			// Ensure that the tree is calculated
			tree.CalculateTree();

			HashSet<int> recursionStack = new();
			OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(original, 0, batches * batchSize));
			int depth = 0, maxDepth = 0;

			if (MagicStorageConfig.IsRecursionEnabled)
				ModifyCraftingTree(availableInventory, recursionStack, orderedTree, ref depth, ref maxDepth, batches);

			return orderedTree;
		}

		private void ModifyCraftingTree(Dictionary<int, int> availableInventory, HashSet<int> recursionStack, OrderedRecipeTree root, ref int depth, ref int maxDepth, int parentBatches) {
			if (!MagicStorageConfig.IsRecursionInfinite && depth >= MagicStorageConfig.RecipeRecursionDepth)
				return;

			// Safety check
			if (tree.Root is null)
				return;

			// Check for infinitely recursive recipe branches (e.g. Wood -> Wood Platform -> Wood)
			if (!recursionStack.Add(tree.originalRecipe.createItem.type))
				return;

			// Ensure that the tree is calculated
			tree.CalculateTree();

			depth++;

			if (depth > maxDepth)
				maxDepth = depth;

			foreach (RecipeIngredientInfo ingredient in tree.Root.info.ingredientTrees) {
				if (ingredient.RecipeCount == 0)
					continue;  // Cannot recurse further, go to next ingredient

				int requiredPerCraft = original.requiredItem[ingredient.recipeIngredientIndex].stack;

				ingredient.FindBestMatchAndSetRecipe(availableInventory);

				Recipe recipe = ingredient.SelectedRecipe;

				int batchSize = recipe.createItem.stack;
				int batches = (int)Math.Ceiling(requiredPerCraft / (double)batchSize * parentBatches);

				OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(recipe, depth, batches * batchSize));
				root.Add(orderedTree);

				if (recipe.TryGetRecursiveRecipe(out var recursive))
					recursive.ModifyCraftingTree(availableInventory, recursionStack, orderedTree, ref depth, ref maxDepth, batches);
			}

			recursionStack.Remove(tree.originalRecipe.createItem.type);

			depth--;
		}

		/// <summary>
		/// Iterate's through this recursive recipe's crafting tree and calculates the maximum amount of this recipe that can be crafted
		/// </summary>
		/// <param name="availableInventory">A dictionary indicating which item types are available and their quantities.  This dictionary <b>will be modified</b> by the time this method finishes executing</param>
		/// <returns>The maximum amount of this recipe that can be crafted, or 0 if <paramref name="availableInventory"/> does not have enough ingredients</returns>
		public int GetMaxCraftable(Dictionary<int, int> availableInventory) {
			ArgumentNullException.ThrowIfNull(availableInventory);

			// Use GetCraftingTree(1) to get the tree (do NOT trim branches, all of the info will be needed!)
			// Then, for each recipe in the tree's processing order, calculate how many crafts are possible and perform them
			// After all of this processing has been handled, the final result will be the amount of crafts possible for the final recipe
			var craftingTree = GetCraftingTree(1);
			var recipeStack = craftingTree.GetProcessingOrder();

			EnvironmentSandbox sandbox;
			IEnumerable<EnvironmentModule> modules;
			if (CraftingGUI.requestingAmountFromUI) {
				var heart = CraftingGUI.GetHeart();

				sandbox = new EnvironmentSandbox(Main.LocalPlayer, heart);
				modules = heart.GetModules();
			} else {
				sandbox = default;
				modules = Array.Empty<EnvironmentModule>();
			}

			OrderedRecipeContext recentContext = null;
			while (recipeStack.TryPop(out OrderedRecipeContext context)) {
				// Calculate how many items can be crafted using this recipe
				Recipe recipe = context.recipe;

				// Local capturing
				var inv = availableInventory;
				Recipe r = recipe;

				int GetMaxCraftsAmount(Item requiredItem) {
					ClampedArithmetic total = 0;

					foreach (var (type, quantity) in inv) {
						if (type == requiredItem.type || CraftingGUI.RecipeGroupMatch(r, type, requiredItem.type))
							total += quantity;
					}

					return total / requiredItem.stack;
				}
				
				int maxCrafts = recipe.requiredItem.Select(GetMaxCraftsAmount).Prepend(int.MaxValue).Min();

				// Overwrite amountToCraft to the actual amount craftable based on the available ingredients
				context.amountToCraft = maxCrafts * recipe.createItem.stack;

				// Consume the ingredients
				Dictionary<int, int> consumedItemCounts = new();
				foreach (Item item in recipe.requiredItem) {
					int required = item.stack * maxCrafts;
					int consume = required;

					// Check the recipe groups
					bool usedRecipeGroup = false;
					foreach (int groupID in recipe.acceptedGroups) {
						RecipeGroup group = RecipeGroup.recipeGroups[groupID];
						if (group.ContainsItem(item.type)) {
							foreach (int groupItem in group.ValidItems) {
								ConsumeItems(availableInventory, groupItem, ref consume, ref usedRecipeGroup, consumedItemCounts);

								if (consume <= 0)
									goto checkNonRecipeGroup;
							}
						}
					}

					checkNonRecipeGroup:

					if (!usedRecipeGroup)
						ConsumeItems(availableInventory, item.type, ref consume, ref usedRecipeGroup, consumedItemCounts);
				}

				// Fake the crafts and add the results to the inventory
				CraftingGUI.CatchDroppedItems = true;
				CraftingGUI.DroppedItems.Clear();

				List<Item> consumedItems = consumedItemCounts.Select(kvp => new Item(kvp.Key, kvp.Value / maxCrafts)).ToList();
				Item createItem = recipe.createItem.Clone();

				for (int i = 0; i < maxCrafts; i++) {
					RecipeLoader.OnCraft(createItem, recipe, consumedItems, new Item());

					foreach (EnvironmentModule module in modules)
						module.OnConsumeItemsForRecipe(sandbox, recipe, consumedItems);
				}

				CraftingGUI.CatchDroppedItems = false;

				availableInventory.AddOrSumCount(recipe.createItem.type, context.amountToCraft);

				foreach (Item item in CraftingGUI.DroppedItems)
					availableInventory.AddOrSumCount(item.type, item.stack);

				// Remember the context, since it won't exist in a collection after this loop
				recentContext = context;
			}

			// Final recipe now has the amount that can be crafted
			if (recentContext is null)
				return 0;

			return recentContext.amountToCraft / recentContext.recipe.createItem.stack;
		}

		private static void ConsumeItems(Dictionary<int, int> availableInventory, int type, ref int consume, ref bool usedRecipeGroup, Dictionary<int, int> consumedItemCounts) {
			int count = availableInventory.TryGetValue(type, out int c) ? c : 0;

			if (count >= consume) {
				usedRecipeGroup = true;
				availableInventory[type] = count - consume;
				consumedItemCounts.AddOrSumCount(type, consume);
				consume = 0;
			} else if (count > 0) {
				usedRecipeGroup = true;
				availableInventory.Remove(type);
				consumedItemCounts.AddOrSumCount(type, count);
				consume -= count;
			}
		}
	}
}
