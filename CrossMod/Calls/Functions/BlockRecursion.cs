using MagicStorage.Common.Systems;
using Terraria;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class BlockRecursion : BaseCallFunctionNoReturn<Recipe> {
		protected override void Handle(Recipe recipe) => MagicCache.BlockRecipeRecursionFor(recipe);
	}
}
