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
			MagicCache.RecursiveRecipesUsingRecipeByIndex.Clear();

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
		/// <param name="available">An optional object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met</param>
		/// <param name="blockedSubrecipeIngredient">An optional item ID representing ingredient trees that should be ignored</param>
		public OrderedRecipeTree GetCraftingTree(int amountToCraft = 1, AvailableRecipeObjects available = null, int blockedSubrecipeIngredient = 0) {
			// Is the main recipe not available?  If so, return an "empty tree"
			if (available?.CanUseRecipe(original) is false)
				return new OrderedRecipeTree(null, 0);

			int batchSize = original.createItem.stack;
			int batches = (int)Math.Ceiling(amountToCraft / (double)batchSize);

			HashSet<int> recursionStack = new();
			OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(original, 0, batches * batchSize), 0);
			int depth = 0, maxDepth = 0;

			if (MagicStorageConfig.IsRecursionEnabled)
				ModifyCraftingTree(available, recursionStack, orderedTree, ref depth, ref maxDepth, batches, blockedSubrecipeIngredient);

			return orderedTree;
		}

		private void ModifyCraftingTree(AvailableRecipeObjects available, HashSet<int> recursionStack, OrderedRecipeTree root, ref int depth, ref int maxDepth, int parentBatches, int ignoreItem) {
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

			depth++;

			if (depth > maxDepth)
				maxDepth = depth;

			foreach (RecipeIngredientInfo ingredient in tree.Root.info.ingredientTrees) {
				if (!ingredient.FindBestMatchAndSetRecipe(available, ignoreItem)) {
					// Cannot recurse further, go to next ingredient
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
					continue;
				}

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
					recursive.ModifyCraftingTree(available, recursionStack, orderedTree, ref depth, ref maxDepth, batches, ignoreItem);
			}

			recursionStack.Remove(createItem);

			depth--;
		}

		/// <summary>
		/// Returns a list of materials needed to craft this recursive recipe
		/// </summary>
		/// <param name="amountToCraft">How many items are expected to be crafted</param>
		/// <param name="result">A structure containing information about the recipes used</param>
		/// <param name="available">
		/// An optional object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met.<br/>
		/// This dictionary is used to trim the crafting tree before getting its required materials.
		/// </param>
		/// <param name="blockedSubrecipeIngredient">An optional item ID representing ingredient trees that should be ignored</param>
		public void GetCraftingInformation(int amountToCraft, out CraftResult result, AvailableRecipeObjects available = null, int blockedSubrecipeIngredient = 0) {
			var craftingTree = GetCraftingTree(amountToCraft, available, blockedSubrecipeIngredient);

			if (available is not null)
				craftingTree.TrimBranches(available);

			craftingTree.GetCraftingInformation(out result);
		}

		/// <summary>
		/// Iterate's through this recursive recipe's crafting tree and calculates the maximum amount of this recipe that can be crafted
		/// </summary>
		/// <param name="available">
		/// An object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met.<br/>
		/// <b>NOTE:</b> the contents of this object may be modified by the time this method call returns
		/// </param>
		/// <returns>The maximum amount of this recipe that can be crafted, or 0 if the information in <paramref name="available"/> could not satisfy any recipes</returns>
		/// <exception cref="ArgumentNullException"/>
		public int GetMaxCraftable(AvailableRecipeObjects available) {
			ArgumentNullException.ThrowIfNull(available);

			var simulation = new CraftingSimulation();
			simulation.SimulateCrafts(this, 9999, available);
			return simulation.AmountCrafted;
		}
	}
}
