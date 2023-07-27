using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public readonly struct ItemInfo {
		public readonly int itemType;
		public readonly int itemStack;

		public ItemInfo(int type, int stack) {
			itemType = type;
			itemStack = stack;
		}

		public void Deconstruct(out int type, out int stack) {
			type = itemType;
			stack = itemStack;
		}
	}

	public class RecurrentRecipeInfo {
		public readonly Recipe recipe;

		public readonly List<ItemInfo> excessResults;

		internal RecurrentRecipeInfo(Recipe recipe) {
			this.recipe = recipe;
			excessResults = new();
		}
	}

	public class RecursiveRecipe {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				recipeToRecursiveRecipe.Clear();
			}
		}

		/// <summary>
		/// The base recipe object
		/// </summary>
		public readonly Recipe original;

		/// <summary>
		/// The tree representing the recipes used to recursive craft this recipe
		/// </summary>
		public readonly RecursionTree tree;

		internal static readonly ConditionalWeakTable<Recipe, RecursiveRecipe> recipeToRecursiveRecipe = new();

		public RecursiveRecipe(Recipe recipe) {
			original = recipe;
			tree = new RecursionTree(recipe);
		}

		public static void RecalculateAllRecursiveRecipes() {
			foreach (var (_, recursive) in recipeToRecursiveRecipe) {
				recursive.tree.Reset();
				recursive.tree.CalculateTree();
			}

			RecursionTree.NodePool.ClearNodes();
		}
	}
}
