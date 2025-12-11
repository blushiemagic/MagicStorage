using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		public static bool IsAvailableForSnapshot(Recipe recipe) => !recipe.Disabled && RecipeLoader.RecipeAvailable(recipe);
	}
}
