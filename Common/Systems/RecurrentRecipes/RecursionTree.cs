using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	/// <summary>
	/// An object representing the full recursive tree for a recipe
	/// </summary>
	public sealed class RecursionTree {
		public readonly Recipe originalRecipe;

		/// <summary>
		/// The node containing the branches for this recursion tree.  If <see cref="originalRecipe"/> is disabled, this will be <see langword="null"/>.
		/// </summary>
		public Node Root { get; private set; }

		public RecursionTree(Recipe recipe) {
			originalRecipe = recipe;
		}

		public void CalculateTree() {
			HashSet<int> processedNodes = new();
			Stack<int> nodeStack = new();
			CalculateTree(processedNodes, nodeStack);
		}

		private void CalculateTree(HashSet<int> processedNodes, Stack<int> nodeStack) {
			if (Root is not null || originalRecipe.Disabled)
				return;

			Root = NodePool.FindOrCreate(originalRecipe);
			
			// Prevent recursion by not checking nodes multiple times
			if (!processedNodes.Add(Root.poolIndex))
				return;

			nodeStack.Push(Root.poolIndex);

			// Process the nodes for each child
			foreach (var ingredientInfo in Root.info.ingredientTrees) {
				foreach (var ingredientRecipe in ingredientInfo.trees) {
					ingredientRecipe.CalculateTree(processedNodes, nodeStack);

					if (!MagicCache.RecursiveRecipesUsingRecipeByIndex.TryGetValue(ingredientRecipe.originalRecipe.RecipeIndex, out var list))
						MagicCache.RecursiveRecipesUsingRecipeByIndex[ingredientRecipe.originalRecipe.RecipeIndex] = list = new();

					// Work up the current tree
					foreach (int index in nodeStack) {
						// Local capturing
						int rootIndex = index;
						if (!list.Any(node => rootIndex == node.poolIndex))
							list.Add(NodePool.Get(index));
					}
				}
			}

			nodeStack.Pop();
		}

		internal void Reset() => Root = null;
	}
}
