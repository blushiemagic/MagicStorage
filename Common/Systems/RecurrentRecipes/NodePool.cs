using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public static class NodePool {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				pool.Clear();
				resultToNodes.Clear();
			}
		}

		private static readonly List<Node> pool = new();
		private static readonly Dictionary<int, List<Node>> resultToNodes = new();

		public static Node FindOrCreate(Recipe recipe) {
			if (recipe.Disabled)
				return null;

			int type = recipe.createItem.type;

			if (!resultToNodes.TryGetValue(type, out var list))
				resultToNodes[type] = list = new();

			Node node = list.Find(n => Utility.RecipesMatchForHistory(recipe, n.info.sourceRecipe));

			if (node is null) {
				int index = pool.Count;
				pool.Add(node = new Node(recipe, index));
				resultToNodes[type].Add(node);
			}

			return node;
		}

		internal static void ClearNodes() {
			pool.Clear();
			resultToNodes.Clear();
		}
	}
}
