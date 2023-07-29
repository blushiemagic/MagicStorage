using System;
using System.Collections.Generic;
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

		private void ModifyCraftingTree(HashSet<int> recursionStack, OrderedRecipeTree root, ref int depth, ref int maxDepth, int parentBatches) {
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
				if (ingredient.trees.Count == 0)
					continue;  // Cannot recurse further, go to next ingredient

				int requiredPerCraft = original.requiredItem[ingredient.recipeIngredientIndex].stack;

				Recipe recipe = ingredient.SelectedRecipe;
				int perCraft = recipe.createItem.stack;

				int batches = (int)Math.Ceiling(requiredPerCraft / (double)perCraft * parentBatches);

				OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(recipe, depth, batches));
				root.Add(orderedTree);

				if (recipe.TryGetRecursiveRecipe(out var recursive))
					recursive.ModifyCraftingTree(recursionStack, orderedTree, ref depth, ref maxDepth, batches);
			}

			recursionStack.Remove(tree.originalRecipe.createItem.type);

			depth--;
		}
	}
}
