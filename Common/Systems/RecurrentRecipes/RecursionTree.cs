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

			public class RecipeInfo : List<List<RecursionTree>> {
				public readonly Recipe sourceRecipe;

				public RecipeInfo(Recipe recipe) : base(InitTree(recipe)) {
					sourceRecipe = recipe;
				}

				private static IEnumerable<List<RecursionTree>> InitTree(Recipe recipe) {
					foreach (var item in recipe.requiredItem)
						yield return new List<RecursionTree>(InitIngredientTree(item.type));
				}

				private static IEnumerable<RecursionTree> InitIngredientTree(int type) {
					if (!MagicCache.ResultToRecipe.TryGetValue(type, out var recipes))
						yield break;

					foreach (var ingredientRecipe in recipes.Where(r => !r.Disabled))
						yield return new RecursionTree(ingredientRecipe);
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
			HashSet<int> processedNodes = new();
			CalculateTree(processedNodes);
		}

		private void CalculateTree(HashSet<int> processedNodes) {
			if (Root is not null)
				return;

			Root = NodePool.FindOrCreate(originalRecipe);
			
			// Prevent recursion by not checking nodes multiple times
			if (!processedNodes.Add(Root.poolIndex))
				return;

			// Process the nodes for each child
			foreach (var ingredientTree in Root.ingredientTrees) {
				foreach (var ingredientRecipe in ingredientTree)
					ingredientRecipe.CalculateTree(processedNodes);
			}
		}

		internal void Reset() => Root = null;
	}
}
