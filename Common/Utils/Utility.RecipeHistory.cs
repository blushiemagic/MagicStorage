using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Terraria;

namespace MagicStorage {
	partial class Utility {
		private class ItemTypeComparer : IEqualityComparer<Item> {
			public static ItemTypeComparer Instance { get; } = new();

			public bool Equals(Item x, Item y) => x.type == y.type;

			public int GetHashCode([DisallowNull] Item obj) => obj.type;
		}

		internal static bool RecipesMatchForHistory(Recipe recipe1, Recipe recipe2) {
			return recipe1.createItem.type == recipe2.createItem.type
				&& recipe1.requiredItem.SequenceEqual(recipe2.requiredItem, ItemTypeComparer.Instance)
				&& recipe1.requiredTile.SequenceEqual(recipe2.requiredTile, EqualityComparer<int>.Default);
		}
	}
}
