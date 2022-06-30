using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	/// <summary>
	/// A class used on items for preventing item combining in a Magic Storage storage system
	/// </summary>
	public abstract class ItemCombining : ModType {
		public int Type { get; private set; }

		private static int NextID;

		internal static Dictionary<int, List<ItemCombining>> combiningObjectsByType;

		public abstract bool CanCombine(Item item1, Item item2);

		public static bool CanCombineItems(Item item1, Item item2) {
			if (!ItemData.Matches(item1, item2))
				return false;

			bool combine = true;

			if (combiningObjectsByType.TryGetValue(item1.type, out var list)) {
				foreach (var obj in list)
					combine &= obj.CanCombine(item1, item2);
			}

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
