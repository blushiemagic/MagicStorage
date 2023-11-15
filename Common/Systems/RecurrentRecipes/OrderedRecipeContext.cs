using System;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class OrderedRecipeContext {
		public readonly Recipe recipe;
		public readonly int depth;
		public readonly SharedCounter amountToCraft;

		private OrderedRecipeTree _source;
		private SharedCounter _ingredientBatcher;
		private SharedCounter[] _childIngredientBatchers;

		public OrderedRecipeContext(Recipe recipe, int depth, SharedCounter counter) {
			this.recipe = recipe;
			this.depth = depth;
			amountToCraft = counter;
			_childIngredientBatchers = new SharedCounter[recipe.requiredItem.Count];
		}

		public void LinkTo(OrderedRecipeTree tree) {
			_source = tree;
		}

		internal SharedCounter RentCounterFromParent(int ingredientStack) {
			int batches = (int)Math.Ceiling(amountToCraft / (double)recipe.createItem.stack) * ingredientStack;

			if (_source?.Root is OrderedRecipeTree parent) {
				ref var parentCounter = ref parent.context._childIngredientBatchers[_source.parentLeafIndex];
				if (parentCounter is null)
					parentCounter = new SharedCounter(batches);
				else
					parentCounter.SetToAtMinimum(batches);

				return parentCounter;
			}

			// Context has no tree or tree has no parent
			if (_ingredientBatcher is null)
				_ingredientBatcher = new SharedCounter(batches);
			else
				_ingredientBatcher.SetToAtMinimum(batches);

			return _ingredientBatcher;
		}

		internal SharedCounter RentIngredientCounter(int index, int stack) {
			ref var counter = ref _childIngredientBatchers[index];

			if (counter is null)
				counter = new SharedCounter(stack);
			else
				counter.SetToAtMinimum(stack);

			return counter;
		}
	}
}
