using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class RecipeIngredientInfo {
		public readonly RecipeInfo parent;

		public readonly int recipeIngredientIndex;

		internal readonly List<RecursionTree> trees;

		/// <summary>
		/// How many recipes can create this ingredient
		/// </summary>
		public int RecipeCount => trees.Count;

		internal RecipeIngredientInfo(RecipeInfo recipeInfo, int index) {
			parent = recipeInfo;
			recipeIngredientIndex = index;

			// Account for recipe groups as well
			int recipeItem = recipeInfo.sourceRecipe.requiredItem[index].type;
			HashSet<int> types = new() { recipeItem };

			foreach (int id in recipeInfo.sourceRecipe.acceptedGroups) {
				RecipeGroup group = RecipeGroup.recipeGroups[id];
				if (group.ContainsItem(recipeItem))
					types.UnionWith(group.ValidItems);
			}

			trees = types.SelectMany(static type => MagicCache.ResultToRecipe.TryGetValue(type, out Recipe[] recipes) ? recipes : Array.Empty<Recipe>())
				.Where(static r => !r.Disabled && !MagicCache.IsRecipeBlocked(r))
				.Select(static r => new RecursionTree(r))
				.ToList();
		}

		internal void ClearTrees() {
			trees.Clear();
		}

		/// <summary>
		/// Returns all possible recipes that can craft this ingredient, given the available inventory
		/// </summary>
		/// <param name="available">
		/// An optional object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met.<br/>
		/// If <see langword="null"/>, all recipes are returned.
		/// </param>
		/// <param name="blockedRecipeIngredient">
		/// An optional item ID representing a recipe ingredient that should not be used when finding the best match.<br/>
		/// If this parameter is greater than 0, then any recipes using the blocked item ID as a possible ingredient will be skipped.
		/// </param>
		public IEnumerable<Recipe> EnumerateValidRecipes(AvailableRecipeObjects available, int blockedRecipeIngredient = 0) {
			if (available is null) {
				// Assume that the caller handles null inventory and use all recipes
				foreach (var tree in trees)
					yield return tree.originalRecipe;

				yield break;
			}

			if (trees.Count < 2) {
				if (trees.Count > 0 && available.CanUseRecipe(trees[0].originalRecipe))
					yield return trees[0].originalRecipe;

				yield break;
			}

			for (int i = 0; i < trees.Count; i++) {
				Recipe subrecipe = trees[i].originalRecipe;

				// Not enough stations or conditions met?  Skip this recipe
				if (!available.CanUseRecipe(subrecipe))
					goto checkNextTree;

				foreach (Item item in subrecipe.requiredItem) {
					bool usedRecipeGroup = false;
					ClampedArithmetic stack = item.stack;

					int count;
					foreach (int groupID in subrecipe.acceptedGroups) {
						RecipeGroup group = RecipeGroup.recipeGroups[groupID];

						// Attempt to use items that are valid in the group
						if (group.ContainsItem(item.type)) {
							foreach (int groupItem in group.ValidItems) {
								if (blockedRecipeIngredient > 0 && groupItem == blockedRecipeIngredient)
									goto checkNextTree;

								if (available.TryGetIngredientQuantity(item.type, out count)) {
									usedRecipeGroup = true;
									stack -= count;

									if (stack <= 0)
										goto checkNonRecipeGroup;
								}
							}
						}
					}

					checkNonRecipeGroup:

					if (!usedRecipeGroup) {
						if (blockedRecipeIngredient > 0 && item.type == blockedRecipeIngredient)
							goto checkNextTree;

						if (available.TryGetIngredientQuantity(item.type, out count))
							stack -= count;
					}

					if (stack > 0) {
						// Recipe ingredient requirement could not be met
						goto checkNextTree;
					}

					if (stack < 0)
						stack = 0;
				}

				// Recipe was fully available
				yield return subrecipe;

				checkNextTree: ;
			}
		}
	}
}
