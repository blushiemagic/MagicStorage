using MagicStorage.Common.Systems;
using SerousCommonLib.API.ModCall;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class AddInfiniteIngredient : BaseCallFunction<int, bool> {
		// "Add Infinite Ingredient", int itemID

		protected override bool Handle(int itemID) => InfiniteItemsForCrafting.AddInfiniteIngredient(itemID);
	}
}
