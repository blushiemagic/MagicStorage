using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class RecipeIngredientInfo {
		public readonly RecipeInfo parent;

		public readonly int recipeIngredientIndex;

		internal readonly List<RecursionTree> trees;

		private int _selectedRecipe;
		/// <summary>
		/// Which recipe should be used to craft this ingredient
		/// </summary>
		public Recipe SelectedRecipe => trees[_selectedRecipe].originalRecipe;

		/// <summary>
		/// Which recursion tree should be used when crafting this ingredient
		/// </summary>
		public RecursionTree SelectedTree => trees[_selectedRecipe];

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
				.Where(static r => !r.Disabled)
				.Select(static r => new RecursionTree(r))
				.ToList();
		}

		public void SetRecipe(int index = -1) {
			_selectedRecipe = index < 0 || index >= trees.Count ? 0 : index;
		}

		/// <summary>
		/// Attempts to update <see cref="SelectedRecipe"/> depending on which possible recipe's ingredients requirement was satisfied the most.<br/>
		/// If no recipe was found as a "best match", <see cref="SelectedRecipe"/> is not updated
		/// </summary>
		/// <param name="availableInventory">A collection of item quantities, indexed by type.  If <see langword="null"/>, <see cref="SelectedRecipe"/> is not updated</param>
		/// <param name="blockedRecipeIngredient">
		/// An optional item ID representing a recipe ingredient that should not be used when finding the best match.<br/>
		/// If this parameter is greater than 0, then any recipes using the blocked item ID as a possible ingredient will be skipped.
		/// </param>
		public void FindBestMatchAndSetRecipe(Dictionary<int, int> availableInventory, int blockedRecipeIngredient = 0) {
			if (availableInventory is null)
				return;

			if (trees.Count < 2) {
				_selectedRecipe = 0;
				return;
			}

			// Attempt to find the recipe with the best "availability", i.e. the recipe that has the most ingredients partially or fully satisfied
			// If one exists, modify the "_selectedRecipe" index to that recipe.  Otherwise, don't modify it
			int bestMatch = -1;
			float bestPercent = 0;

			for (int i = 0; i < trees.Count; i++) {
				Recipe subrecipe = trees[i].originalRecipe;

				float percent = 0;
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

								if (availableInventory.TryGetValue(item.type, out count)) {
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

						if (availableInventory.TryGetValue(item.type, out count))
							stack -= count;
					}

					if (stack < 0)
						stack = 0;

					float stackConsumedFactor = (float)(item.stack - stack) / item.stack;
					percent += stackConsumedFactor / subrecipe.requiredItem.Count;
				}

				if (percent > bestPercent) {
					bestMatch = i;
					bestPercent = percent;
				}

				checkNextTree: ;
			}

			// Update the initially selected recipe
			if (bestMatch >= 0)
				_selectedRecipe = bestMatch;
		}
	}
}
