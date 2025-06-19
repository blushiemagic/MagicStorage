using MagicStorage.Common.Systems;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class HideRecipeResult : BaseCallFunction<int, bool> {
		protected override bool Handle(int resultItem) => HiddenRecipes.HideRecipeResult(resultItem);
	}
}
