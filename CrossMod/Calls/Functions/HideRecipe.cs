using MagicStorage.Common.Systems;
using SerousCommonLib.API.ModCall;
using Terraria;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class HideRecipe : BaseCallFunction<Recipe, bool> {
		// "Hide Recipe", Recipe recipe

		protected override bool Handle(Recipe recipe) => HiddenRecipes.HideRecipe(recipe);
	}
}
