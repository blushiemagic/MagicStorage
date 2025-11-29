using MagicStorage.Common.Systems;
using SerousCommonLib.API.ModCall;
using Terraria;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class IsRecipeHidden : BaseCallFunction<Recipe, bool> {
		// "Is Recipe Hidden", Recipe recipe

		protected override bool Handle(Recipe recipe) => HiddenRecipes.IsHidden(recipe);
	}
}
