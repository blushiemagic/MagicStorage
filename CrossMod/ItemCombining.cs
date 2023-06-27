using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	/// <summary>
	/// A class used on items for preventing item combining in a Magic Storage storage system
	/// </summary>
	public abstract class ItemCombining : ModType {
		public int Type { get; private set; }

		internal static int NextID;

		internal static Dictionary<int, List<ItemCombining>> combiningObjectsByType;

		public abstract bool CanCombine(Item item1, Item item2);

		public static bool CanCombineItems(Item item1, Item item2, bool checkPrefix = true) => CanCombineItems(item1, item2, checkPrefix, null);

		public static bool CanCombineItems(Item item1, Item item2, bool checkPrefix, ConditionalWeakTable<Item, byte[]> savedItemTagIO) {
			int prefix1 = item1.prefix;
			int prefix2 = item2.prefix;

			if (!checkPrefix) {
				item1.prefix = 0;
				item2.prefix = 0;
			}

			if ((checkPrefix && !ItemData.Matches(item1, item2)) || item1.type != item2.type) {
				item1.prefix = prefix1;
				item2.prefix = prefix2;

				return false;
			}

			bool combine = true;

			//Ignore favorite
			bool oldFavorite1 = item1.favorited;
			bool oldFavorite2 = item2.favorited;

			combiningObjectsByType ??= new();

			if (combiningObjectsByType.TryGetValue(item1.type, out var list)) {
				foreach (var obj in list)
					combine &= obj.CanCombine(item1, item2);
			}

			//Support tML hooks
			combine &= ItemLoader.CanStack(item1, item2);

			//Regardless of if the above allows combining, prevent items with different ModItemData from combining if they aren't the same
			if (combine)
				combine &= Utility.AreStrictlyEqual(item1, item2, checkStack: false, checkPrefix: checkPrefix, savedItemTagIO: savedItemTagIO);

			item1.prefix = prefix1;
			item2.prefix = prefix2;

			item1.favorited = oldFavorite1;
			item2.favorited = oldFavorite2;

			return combine;
		}

		public abstract int TargetItemType { get; }

		protected sealed override void Register() {
			ModTypeLookup<ItemCombining>.Register(this);
			Type = NextID++;

			combiningObjectsByType ??= new();

			if (!combiningObjectsByType.TryGetValue(TargetItemType, out var list))
				list = combiningObjectsByType[TargetItemType] = new();

			list.Add(this);
		}
	}
}
