using MagicStorage.Common.Systems.RecurrentRecipes;
using Terraria;

namespace MagicStorage {
	partial class Utility {
		public static bool HasRecursiveRecipe(this Recipe recipe) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out _);

		public static RecursiveRecipe GetRecursiveRecipe(this Recipe recipe) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out RecursiveRecipe recursive) ? recursive : null;

		public static bool TryGetRecursiveRecipe(this Recipe recipe, out RecursiveRecipe recursive) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out recursive);
	}
}
