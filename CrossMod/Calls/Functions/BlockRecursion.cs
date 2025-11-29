using MagicStorage.Common.Systems;
using SerousCommonLib.API.ModCall;
using Terraria;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class BlockRecursion : BaseCallFunctionNoReturn<Recipe> {
		// "Block Recursion", Recipe recipe

		protected override void Handle(Recipe recipe) => MagicCache.BlockRecipeRecursionFor(recipe);
	}
}
