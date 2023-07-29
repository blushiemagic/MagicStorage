using System;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class OrderedRecipeTree {
		private readonly List<OrderedRecipeTree> leaves = new();
		public readonly OrderedRecipeContext context;

		public IReadOnlyList<OrderedRecipeTree> Leaves => leaves;

		public OrderedRecipeTree Root { get; private set; }

		public OrderedRecipeTree(OrderedRecipeContext context) {
			this.context = context;
		}

		public void Add(OrderedRecipeTree tree) {
			leaves.Add(tree);
			tree.Root = this;
		}

		public void AddRange(IEnumerable<OrderedRecipeTree> trees) {
			foreach (var tree in trees) {
				leaves.Add(tree);
				tree.Root = this;
			}
		}

		public void Clear() {
			context.amountToCraft = 0;
			leaves.Clear();
		}

		/// <summary>
		/// Trims the branches of any trees whose ingredient requirement is met.<br/>
		/// Use <see cref="GetProcessingOrder"/> to get the updated order of the recipe contexts.
		/// </summary>
		/// <param name="getItemCountForIngredient">A function taking the recipe, the ingredient type and returning how many items can satisfy that ingredient requirement</param>
		public void TrimBranches(Func<Recipe, int, int> getItemCountForIngredient) {
			NetHelper.Report(true, "Trimming branches of recipe tree...");

			// Go from the top of the tree down, cutting off any branches when necessary
			Queue<OrderedRecipeTree> queue = new();
			foreach (var leaf in leaves)
				queue.Enqueue(leaf);

			int depth;
			while (queue.TryDequeue(out OrderedRecipeTree branch)) {
				// Check if the amount needed has been satisfied
				// If it is, this recipe and its children are not needed
				Recipe recipe = branch.context.recipe;

				int result = recipe.createItem.type;
				ref int remaining = ref branch.context.amountToCraft;

				remaining -= getItemCountForIngredient(recipe, result);

				depth = branch.context.depth;
				if (remaining <= 0) {
					NetHelper.Report(false, $"Branch trimmed: Depth = {depth}, Recipe result = {recipe.createItem.stack} {Lang.GetItemNameValue(result)}");

					branch.Clear();
				} else {
					// Check the leaves
					foreach (var leaf in branch.Leaves)
						queue.Enqueue(leaf);
				}
			}
		}

		public Stack<OrderedRecipeContext> GetProcessingOrder() {
			Stack<OrderedRecipeContext> recipeStack = new();
			Queue<OrderedRecipeTree> treeQueue = new();
			treeQueue.Enqueue(this);

			while (treeQueue.TryDequeue(out OrderedRecipeTree branch)) {
				recipeStack.Push(branch.context);

				foreach (var leaf in branch.Leaves)
					treeQueue.Enqueue(leaf);
			}

			return recipeStack;
		}
	}
}
