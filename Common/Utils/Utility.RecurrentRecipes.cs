using MagicStorage.Common.Systems.RecurrentRecipes;
using Terraria;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Checks whether a recipe has recursive recipe metadata.
		/// </summary>
		/// <param name="recipe">The recipe to inspect.</param>
		/// <returns><see langword="true" /> when recursive metadata exists; otherwise, <see langword="false" />.</returns>
		public static bool HasRecursiveRecipe(this Recipe recipe) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out _);

		/// <summary>
		/// Gets recursive recipe metadata for a recipe.
		/// </summary>
		/// <param name="recipe">The recipe to inspect.</param>
		/// <returns>The recursive recipe metadata, or <see langword="null" /> when none exists.</returns>
		public static RecursiveRecipe GetRecursiveRecipe(this Recipe recipe) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out RecursiveRecipe recursive) ? recursive : null;

		/// <summary>
		/// Attempts to get recursive recipe metadata for a recipe.
		/// </summary>
		/// <param name="recipe">The recipe to inspect.</param>
		/// <param name="recursive">The recursive recipe metadata, when present.</param>
		/// <returns><see langword="true" /> when recursive metadata exists; otherwise, <see langword="false" />.</returns>
		public static bool TryGetRecursiveRecipe(this Recipe recipe, out RecursiveRecipe recursive) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out recursive);
	}
}
