using MagicStorage.Common.Systems;
using SerousCommonLib.API.ModCall;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class HideRecipeResult : BaseCallFunction<int, bool> {
		// "Hide Recipe Result", int resultItem

		protected override bool Handle(int resultItem) => HiddenRecipes.HideRecipeResult(resultItem);
	}
}
