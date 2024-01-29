using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class Node {
		public readonly int poolIndex;

		public readonly RecipeInfo info;

		internal Node(Recipe recipe, int index) {
			poolIndex = index;

			info = new RecipeInfo(recipe);
		}

		internal void ClearTrees() {
			info.ClearTrees();
		}
	}
}
