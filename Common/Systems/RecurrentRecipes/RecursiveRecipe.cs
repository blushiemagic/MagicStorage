using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
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
		/// Returns a tree representing the recipes this recursive recipe will use and their expected crafted quantities
		/// </summary>
		/// <param name="amountToCraft">How many items are expected to be crafted.  Defaults to 1</param>
		/// <param name="availableInventory">An optional dictionary indicating which item types are available and their quantities</param>
		/// <param name="blockedSubrecipeIngredient">An optional item ID representing ingredient trees that should be ignored</param>
		public OrderedRecipeTree GetCraftingTree(int amountToCraft = 1, Dictionary<int, int> availableInventory = null, int blockedSubrecipeIngredient = 0) {
			int batchSize = original.createItem.stack;
			int batches = (int)Math.Ceiling(amountToCraft / (double)batchSize);

			// Ensure that the tree is calculated
			tree.CalculateTree();

			HashSet<int> recursionStack = new();
			OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(original, 0, batches * batchSize), 0);
			int depth = 0, maxDepth = 0;

			if (MagicStorageConfig.IsRecursionEnabled)
				ModifyCraftingTree(availableInventory, recursionStack, orderedTree, ref depth, ref maxDepth, batches, blockedSubrecipeIngredient);

			return orderedTree;
		}

		private void ModifyCraftingTree(Dictionary<int, int> availableInventory, HashSet<int> recursionStack, OrderedRecipeTree root, ref int depth, ref int maxDepth, int parentBatches, int ignoreItem) {
			if (!MagicStorageConfig.IsRecursionInfinite && depth >= MagicStorageConfig.RecipeRecursionDepth)
				return;

			// Safety check
			if (tree.Root is null)
				return;

			// Check for infinitely recursive recipe branches (e.g. Wood -> Wood Platform -> Wood)
			// Also, if the created item is blocked by UI logic, block this recipe
			int createItem = tree.originalRecipe.createItem.type;
			if (!recursionStack.Add(createItem))
				return;

			// Ensure that the tree is calculated
			tree.CalculateTree();

			depth++;

			if (depth > maxDepth)
				maxDepth = depth;

			foreach (RecipeIngredientInfo ingredient in tree.Root.info.ingredientTrees) {
				if (ingredient.RecipeCount == 0)
					continue;  // Cannot recurse further, go to next ingredient

				ingredient.FindBestMatchAndSetRecipe(availableInventory);

				Recipe recipe = ingredient.SelectedRecipe;
				
				// Block recursion that would require the blocked item type
				if (ignoreItem > 0) {
					bool nextIngredient = false;
					foreach (Item item in recipe.requiredItem) {
						if (ignoreItem == item.type || CraftingGUI.RecipeGroupMatch(recipe, ignoreItem, item.type)) {
							nextIngredient = true;
							break;
						}
					}

					if (nextIngredient)
						continue;
				}

				Recipe sourceRecipe = ingredient.parent.sourceRecipe;
				Item requiredItem = sourceRecipe.requiredItem[ingredient.recipeIngredientIndex];
				
				int requiredPerCraft = requiredItem.stack;

				int batchSize = recipe.createItem.stack;
				int batches = (int)Math.Ceiling(requiredPerCraft / (double)batchSize * parentBatches);

				// Any extras above the required amount will either end up recycled by other subrecipes or be extra results
				int amountToCraft = Math.Max(requiredPerCraft, batches * batchSize);

				OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(recipe, depth, amountToCraft), ingredient.recipeIngredientIndex);
				root.Add(orderedTree);

				if (recipe.TryGetRecursiveRecipe(out var recursive))
					recursive.ModifyCraftingTree(availableInventory, recursionStack, orderedTree, ref depth, ref maxDepth, batches, ignoreItem);
			}

			recursionStack.Remove(createItem);

			depth--;
		}

		/// <summary>
		/// Returns a list of materials needed to craft this recursive recipe
		/// </summary>
		/// <param name="amountToCraft">How many items are expected to be crafted</param>
		/// <param name="result">A structure containing information about the recipes used</param>
		/// <param name="availableInventory">
		/// An optional dictionary indicating which item types are available and their quantities.<br/>
		/// This dictionary is used to trim the crafting tree before getting its required materials.
		/// </param>
		/// <param name="blockedSubrecipeIngredient">An optional item ID representing ingredient trees that should be ignored</param>
		public void GetCraftingInformation(int amountToCraft, out CraftResult result, Dictionary<int, int> availableInventory = null, int blockedSubrecipeIngredient = 0) {
			var craftingTree = GetCraftingTree(amountToCraft, availableInventory, blockedSubrecipeIngredient);

			if (availableInventory is not null) {
				// Local capturing
				var inv = availableInventory;
				craftingTree.TrimBranches((recipe, result) => GetItemCount(recipe, result, inv));
			}

			craftingTree.GetCraftingInformation(out result);
		}

		private static int GetItemCount(Recipe recipe, int result, Dictionary<int, int> availableInventory) {
			ClampedArithmetic count = 0;
			bool useRecipeGroup = false;
			foreach (var (item, quantity) in availableInventory) {
				if (CraftingGUI.RecipeGroupMatch(recipe, item, result)) {
					count += quantity;
					useRecipeGroup = true;
				}
			}

			if (!useRecipeGroup && availableInventory.TryGetValue(result, out int amount))
				count += amount;

			return count;
		}

		/// <summary>
		/// Iterate's through this recursive recipe's crafting tree and calculates the maximum amount of this recipe that can be crafted
		/// </summary>
		/// <param name="availableInventory">
		/// A dictionary indicating which item types are available and their quantities.<br/>
		/// <b>NOTE:</b> the contents of this dictionary may be modified by the time this method call returns
		/// </param>
		/// <returns>The maximum amount of this recipe that can be crafted, or 0 if <paramref name="availableInventory"/> does not have enough ingredients</returns>
		/// <exception cref="ArgumentNullException"/>
		public int GetMaxCraftable(Dictionary<int, int> availableInventory) {
			ArgumentNullException.ThrowIfNull(availableInventory);

			var simulation = new CraftingSimulation();
			simulation.SimulateCrafts(this, 9999, availableInventory);
			return simulation.AmountCrafted;
		}
	}
}
