using MagicStorage.Common.Systems;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class AddInfiniteIngredient : BaseCallFunction<int, bool> {
		protected override bool Handle(int itemID) => InfiniteItemsForCrafting.AddInfiniteIngredient(itemID);
	}
}
