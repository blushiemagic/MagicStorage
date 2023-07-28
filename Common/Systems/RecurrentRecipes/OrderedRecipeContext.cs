using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class OrderedRecipeContext {
		public readonly Recipe recipe;
		public readonly int depth;
		public int amountToCraft;

		public OrderedRecipeContext(Recipe recipe, int depth, int amountCrafted) {
			this.recipe = recipe;
			this.depth = depth;
			this.amountToCraft = amountCrafted;
		}
	}
}
