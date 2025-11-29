using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems {
	/// <summary>
	/// A class used to register recipes that should never appear in the Crafting Interface.
	/// </summary>
	public class HiddenRecipes : ModSystem {
		private static readonly List<Recipe> _hiddenRecipes = [];
		private static readonly HashSet<int> _hiddenRecipeResults = [];

		public override void Load() {
			_hiddenRecipes.Clear();
			_hiddenRecipeResults.Clear();
		}

		/// <summary>
		/// Adds the given recipe to the list of hidden recipes.
		/// </summary>
		public static bool HideRecipe(Recipe recipe) {
			if (!_hiddenRecipes.Contains(recipe)) {
				_hiddenRecipes.Add(recipe);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Adds the given item ID to the list of hidden recipe results.<br/>
		/// All recipes that create this item will be hidden.
		/// </summary>
		public static bool HideRecipeResult(int resultItem) => _hiddenRecipeResults.Add(resultItem);

		public static bool IsHidden(Recipe recipe) => recipe is not null && _hiddenRecipeResults.Contains(recipe.createItem.type) || _hiddenRecipes.Contains(recipe);

		internal static bool IsVisible(Recipe recipe) => !IsHidden(recipe);
	}
}
