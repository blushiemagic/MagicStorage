using System.Collections.Generic;

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
	}
}
