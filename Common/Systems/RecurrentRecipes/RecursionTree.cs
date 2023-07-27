using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	/// <summary>
	/// An object representing the full recursive tree for a recipe
	/// </summary>
	public sealed class RecursionTree {
		public readonly Recipe originalRecipe;

		public Node Root { get; private set; }

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
			foreach (var ingredientInfo in Root.info.ingredientTrees) {
				foreach (var ingredientRecipe in ingredientInfo.trees)
					ingredientRecipe.CalculateTree(processedNodes);
			}
		}

		internal void Reset() => Root = null;
	}
}
