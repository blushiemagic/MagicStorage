using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Threading {
	public class ItemInfoListProvider {
		public readonly ListProvider<Item> items;
		public readonly ListProvider<ItemInfo> info;

		public ItemInfoListProvider(
			List<Item> staticItemsList,
			List<ItemInfo> staticInfoList
		) {
			items = new ListProvider<Item>(staticItemsList);
			info = new ListProvider<ItemInfo>(staticInfoList);
		}
	}
}
