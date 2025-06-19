using MagicStorage.Common.Systems;
using Terraria;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class IsRecipeHidden : BaseCallFunction<Recipe, bool> {
		protected override bool Handle(Recipe recipe) => HiddenRecipes.IsHidden(recipe);
	}
}
