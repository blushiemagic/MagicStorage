using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class RecipeIngredientInfo {
		public readonly Recipe sourceRecipe;

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

		internal RecipeIngredientInfo(Recipe recipe, int index) {
			sourceRecipe = recipe;
			recipeIngredientIndex = index;

			// Account for recipe groups as well
			int recipeItem = recipe.requiredItem[index].type;
			HashSet<int> types = new() { recipeItem };

			foreach (int id in recipe.acceptedGroups) {
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
	}
}
