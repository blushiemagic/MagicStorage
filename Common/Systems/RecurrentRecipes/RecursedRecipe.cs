using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class RecursedRecipe {
		public readonly int recursionDepth;
		public readonly Recipe recipe;

		public RecursedRecipe(int recursionDepth, Recipe recipe) {
			this.recursionDepth = recursionDepth;
			this.recipe = recipe;
		}
	}

	public sealed class RecursedRecipeComparer : IEqualityComparer<RecursedRecipe> {
		public static RecursedRecipeComparer Instance { get; } = new();

		public bool Equals(RecursedRecipe x, RecursedRecipe y) {
			return x.recursionDepth == y.recursionDepth && object.ReferenceEquals(x.recipe, y.recipe);
		}

		public int GetHashCode([DisallowNull] RecursedRecipe obj) {
			return obj.recipe.GetHashCode();
		}
	}
}
