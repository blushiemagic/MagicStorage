using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	/// <summary>
	/// An object representing the full recursive tree for a recipe
	/// </summary>
	public class RecursionTree {
		public static class NodePool {
			private class Loadable : ILoadable {
				public void Load(Mod mod) { }

				public void Unload() {
					pool.Clear();
					resultToNodes.Clear();
				}
			}

			public class RecipeInfo : List<RecursionTree> {
				public readonly Recipe sourceRecipe;

				public RecipeInfo(Recipe recipe) : base(InitTree(recipe)) {
					sourceRecipe = recipe;
				}

				private static IEnumerable<RecursionTree> InitTree(Recipe recipe) {
					foreach (var item in recipe.requiredItem) {
						if (!MagicCache.ResultToRecipe.TryGetValue(item.type, out var recipes))
							continue;

						foreach (var ingredientRecipe in recipes.Where(r => !r.Disabled))
							yield return new RecursionTree(ingredientRecipe);
					}
				}
			}

			public class Node {
				public readonly int poolIndex;

				public readonly RecipeInfo ingredientTrees;

				internal Node(Recipe recipe, int index) {
					poolIndex = index;

					ingredientTrees = new RecipeInfo(recipe);
				}
			}

			private static readonly List<Node> pool = new();
			private static readonly Dictionary<int, List<Node>> resultToNodes = new();

			public static Node FindOrCreate(Recipe recipe) {
				if (recipe.IsRecursiveRecipe() || recipe.Disabled)
					return null;

				int type = recipe.createItem.type;

				if (!resultToNodes.TryGetValue(type, out var list))
					resultToNodes[type] = list = new();

				Node node = list.Find(n => Utility.RecipesMatchForHistory(recipe, n.ingredientTrees.sourceRecipe));

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
		
		public readonly Recipe originalRecipe;

		public NodePool.Node Root { get; private set; }

		public RecursionTree(Recipe recipe) {
			originalRecipe = recipe;
		}

		public void CalculateTree() {
			if (Root is not null)
				return;  // Tree was already calculated

			HashSet<int> processedNodes = new();

			Root = NodePool.FindOrCreate(originalRecipe);

			Queue<NodePool.Node> nodeQueue = new();
			nodeQueue.Enqueue(Root);

			// Process the full tree, bottom-up
			while (nodeQueue.TryDequeue(out var node)) {
				// Prevent recursion by not checking nodes multiple times
				if (node is null || !processedNodes.Add(node.poolIndex))
					continue;

				foreach (var ingredient in node.ingredientTrees) {
					ingredient.CalculateTree();

					nodeQueue.Enqueue(ingredient.Root);
				}
			}
		}

		internal void Reset() => Root = null;
	}
}
