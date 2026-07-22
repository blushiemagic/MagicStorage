using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Provides synchronized item and item-info working lists.
	/// </summary>
	public class ItemInfoListProvider {
		/// <summary>
		/// Gets the item list provider.
		/// </summary>
		public readonly ListProvider<Item> items;

		/// <summary>
		/// Gets the item-info list provider.
		/// </summary>
		public readonly ListProvider<ItemInfo> info;

		/// <summary>
		/// Creates providers for the specified static item and info lists.
		/// </summary>
		/// <param name="staticItemsList">The static item list.</param>
		/// <param name="staticInfoList">The static item-info list.</param>
		public ItemInfoListProvider(
			List<Item> staticItemsList,
			List<ItemInfo> staticInfoList
		) {
			items = new ListProvider<Item>(staticItemsList);
			info = new ListProvider<ItemInfo>(staticInfoList);
		}
	}
}
