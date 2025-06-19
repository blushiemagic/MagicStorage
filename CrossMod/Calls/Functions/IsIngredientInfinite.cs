using MagicStorage.Common.Systems;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class IsIngredientInfinite : BaseCallFunction<int, bool> {
		protected override bool Handle(int itemID) => InfiniteItemsForCrafting.IsConsideredInfinite(itemID);
	}
}
