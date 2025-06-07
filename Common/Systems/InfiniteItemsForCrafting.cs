using System.Collections.Generic;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems {
	/// <summary>
	/// A class used to track item IDs which should not be consumed when crafting in a Crafting Interface.
	/// </summary>
	public class InfiniteItemsForCrafting : ModSystem {
		private static readonly HashSet<int> _infiniteItems = [];

		public override void Load() {
			_infiniteItems.Clear();
		}

		/// <summary>
		/// Adds the item with the given ID to the list of infinite items.
		/// </summary>
		public static bool AddInfiniteIngredient(int type) => _infiniteItems.Add(type);

		/// <summary>
		/// Whether the item with the given ID is considered infinite.
		/// </summary>
		public static bool IsConsideredInfinite(int type) => _infiniteItems.Contains(type);

		public static HashSet<int> GetInfiniteItems() => [.. _infiniteItems];
	}
}
