using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public readonly struct ItemInfo {
		public readonly int itemType;
		public readonly int itemStack;

		public ItemInfo(int type, int stack) {
			itemType = type;
			itemStack = stack;
		}

		public void Deconstruct(out int type, out int stack) {
			type = itemType;
			stack = itemStack;
		}
	}

	public sealed class RecurrentRecipeInfo {
		public readonly Recipe recipe;

		public readonly List<ItemInfo> excessResults;

		internal RecurrentRecipeInfo(Recipe recipe) {
			this.recipe = recipe;
			excessResults = new();
		}
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
		/// Returns the order that the recipes for this recursive recipe should be processed in
		/// </summary>
		/// <param name="amountToCraft">How many items are expected to be crafted</param>
		public OrderedRecipeTree GetCraftingTree(int amountToCraft) {
			int batchSize = original.createItem.stack;
			int batches = (int)Math.Ceiling(amountToCraft / (double)batchSize);

			HashSet<int> recursionStack = new();
			OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(original, 0, batches * batchSize));
			int depth = 0, maxDepth = 0;

			ModifyCraftingTree(recursionStack, orderedTree, ref depth, ref maxDepth, batches);

			return orderedTree;
		}

		// TODO: the below comment
		/*   CraftingGUI should go through the order and calculate "how many" of each sub-recipe is needed to craft the requested amount of the final recipe
		 *       - logic for that will reverse this reversed list
		 *       - slower, but making the API sensible is more important
		 *   It should then go through the order, skipping those which are 0 or less, and attempt to craft that much
		 *   If any recipe could not be crafted, the logic should just move on to the next (any recipes further up in the tree will fail as well)
		 */

		private void ModifyCraftingTree(HashSet<int> recursionStack, OrderedRecipeTree root, ref int depth, ref int maxDepth, int parentBatches) {
			// Safety check
			if (tree.Root is null)
				return;

			// Check for infinitely recursive recipe branches (e.g. Wood -> Wood Platform -> Wood)
			if (!recursionStack.Add(tree.Root.poolIndex))
				return;

			// Ensure that the tree is calculated
			tree.CalculateTree();

			depth++;

			if (depth > maxDepth)
				maxDepth = depth;

			foreach (RecipeIngredientInfo ingredient in tree.Root.info.ingredientTrees) {
				if (ingredient.trees.Count == 0)
					continue;  // Cannot recurse further, go to next ingredient

				int requiredPerCraft = original.requiredItem[ingredient.recipeIngredientIndex].stack;

				Recipe recipe = ingredient.SelectedRecipe;

				int amountToCraft = parentBatches * recipe.createItem.stack;
				int batches = (int)Math.Ceiling(amountToCraft / (double)requiredPerCraft);

				OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(recipe, depth, amountToCraft));
				root.Add(orderedTree);

				if (recipe.TryGetRecursiveRecipe(out var recursive))
					recursive.ModifyCraftingTree(recursionStack, orderedTree, ref depth, ref maxDepth, batches);
			}

			recursionStack.Remove(tree.Root.poolIndex);

			depth--;
		}
	}
}
