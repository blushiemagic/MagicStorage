using MagicStorage.Common.Systems;
using SerousCommonLib.API.ModCall;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class IsIngredientInfinite : BaseCallFunction<int, bool> {
		protected override bool Handle(int itemID) => InfiniteItemsForCrafting.IsConsideredInfinite(itemID);
	}
}
