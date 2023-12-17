using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria;
using MagicStorage.Common.Threading;
using System.Collections.Concurrent;
using System.Linq;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public static class NodePool {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				pool.Clear();
				resultToNodes.Clear();
			}
		}

		private static readonly ConcurrentDictionary<int, Node> pool = new();
		private static readonly ConcurrentDictionary<int, List<Node>> resultToNodes = new();

		internal static Node Get(int index) => pool[index];

		public static Node FindOrCreate(Recipe recipe) {
			if (recipe.Disabled)
				return null;

			int type = recipe.createItem.type;

			var list = resultToNodes.GetOrAdd(type, static _ => new());

			lock (list) {
				Node node = list.FirstOrDefault(n => Utility.RecipesMatchForHistory(recipe, n.info.sourceRecipe));

				if (node is null) {
					int index = pool.Count;
					pool.TryAdd(index, node = new Node(recipe, index));
					resultToNodes[type].Add(node);
				}

				return node;
			}
		}

		internal static void ClearNodes() {
			pool.Clear();
			resultToNodes.Clear();
		}
	}
}
