using MagicStorage.Common.Threading;
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
			Stack<Node> nodeStack = new();
			HashSet<int> activeNodes = new();
			Root = NodePool.FindOrCreate(originalRecipe);
			CalculateTree(Root, nodeStack, activeNodes, out _);
		}

		private static void CalculateTree(Node root, Stack<Node> nodeStack, HashSet<int> activeNodes, out int[] dependencyIndexes) {
			dependencyIndexes = [];

			if (root is null)
				return;

			Recipe recipe = root.info.sourceRecipe;
			if (recipe.Disabled)
				return;
			
			// Prevent cycles only on the active path. Shared subtrees still need
			// to register every current ancestor in the reverse dependency index.
			if (!activeNodes.Add(root.poolIndex))
				return;

			// Block recipes that should be blocked
			if (MagicCache.IsRecipeBlocked(recipe)) {
				activeNodes.Remove(root.poolIndex);
				return;
			}

			nodeStack.Push(root);

			if (root.DependenciesInitialized) {
				dependencyIndexes = root.DependencyRecipeIndexes;
				RegisterReverseDependencies(dependencyIndexes, nodeStack);

				nodeStack.Pop();
				activeNodes.Remove(root.poolIndex);
				return;
			}

			HashSet<int> dependencyRecipeIndexes = new();

			// Process the nodes for each child
			foreach (RecipeIngredientInfo ingredientInfo in root.info.ingredientTrees) {
				foreach (Node ingredientNode in ingredientInfo.GetOrCreateNodes()) {
					int recipeIndex = ingredientNode.info.sourceRecipe.RecipeIndex;
					dependencyRecipeIndexes.Add(recipeIndex);
					RegisterDirectParent(recipeIndex, recipe.RecipeIndex);

					CalculateTree(ingredientNode, nodeStack, activeNodes, out int[] nestedDependencyIndexes);
					foreach (int nestedRecipeIndex in nestedDependencyIndexes)
						dependencyRecipeIndexes.Add(nestedRecipeIndex);
				}
			}

			dependencyIndexes = [.. dependencyRecipeIndexes];
			root.InitializeDependencyRecipeIndexes(dependencyRecipeIndexes);

			RegisterReverseDependencies(dependencyIndexes, nodeStack);

			nodeStack.Pop();
			activeNodes.Remove(root.poolIndex);
		}

		internal void Reset() {
			Root?.ClearTrees();
			Root = null;
		}

		private static void RegisterDirectParent(int childRecipeIndex, int parentRecipeIndex) {
			HashSet<int> set;
			if (WorkManager.IsWorking) {
				set = MagicCache.concurrentDirectRecursiveRecipeParentsByRecipeIndex.GetOrAdd(childRecipeIndex, static _ => new());
			} else {
				if (!MagicCache.DirectRecursiveRecipeParentsByRecipeIndex.TryGetValue(childRecipeIndex, out set))
					MagicCache.DirectRecursiveRecipeParentsByRecipeIndex[childRecipeIndex] = set = new();
			}

			lock (set) {
				set.Add(parentRecipeIndex);
			}
		}

		private static void RegisterReverseDependencies(int[] recipeIndexes, Stack<Node> nodeStack) {
			foreach (int recipeIndex in recipeIndexes) {
				HashSet<Node> set;
				if (WorkManager.IsWorking) {
					set = MagicCache.concurrentRecursiveRecipesUsingRecipeByIndex.GetOrAdd(recipeIndex, static _ => new(ReferenceEqualityComparer.Instance));
				} else {
					if (!MagicCache.RecursiveRecipesUsingRecipeByIndex.TryGetValue(recipeIndex, out set))
						MagicCache.RecursiveRecipesUsingRecipeByIndex[recipeIndex] = set = new(ReferenceEqualityComparer.Instance);
				}

				lock (set) {
					// Work up the current tree
					foreach (Node node in nodeStack)
						set.Add(node);
				}
			}
		}
	}
}
