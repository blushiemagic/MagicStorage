using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class RecipeInfo {
		public readonly Recipe sourceRecipe;

		internal readonly List<RecipeIngredientInfo> ingredientTrees;

		public RecipeInfo(Recipe recipe) {
			sourceRecipe = recipe;
			ingredientTrees = new();

			for (int i = 0; i < recipe.requiredItem.Count; i++)
				ingredientTrees.Add(new RecipeIngredientInfo(this, i));
		}
	}
}
