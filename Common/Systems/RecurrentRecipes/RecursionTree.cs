using System.Collections.Generic;
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
			CalculateTree(processedNodes);
		}

		private void CalculateTree(HashSet<int> processedNodes) {
			if (Root is not null || originalRecipe.Disabled)
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
