using MagicStorage.CrossMod;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	/// <summary>
	/// A class used on items for preventing item combining in a Magic Storage storage system
	/// </summary>
	[Obsolete("Use StorageAggregator instead", error: true)]
	public abstract class ItemCombining : ModType {
		[Autoload(false)]
		private class AggregatorWrapper : StorageAggregator {
			public readonly ItemCombining combiner;

			public AggregatorWrapper(ItemCombining combiner) {
				this.combiner = combiner;
			}

			public override bool AppliesToItem(Item item) => item.type == combiner.TargetItemType;

			public override bool? CanAggregateItems(Item destination, Item checking) => combiner.CanCombine(destination, checking) ? null : false;
		}

		public int Type { get; private set; }

		internal static int NextID;

		public abstract bool CanCombine(Item item1, Item item2);

		public static bool CanCombineItems(Item item1, Item item2, bool checkPrefix = true) => StorageAggregator.CanCombineItems(item1, item2, checkPrefix);

		public static bool CanCombineItems(Item item1, Item item2, bool checkPrefix, ConditionalWeakTable<Item, byte[]> savedItemTagIO) => StorageAggregator.CanCombineItems(item1, item2, checkPrefix, false, savedItemTagIO);

		public abstract int TargetItemType { get; }

		protected sealed override void Register() {
			ModTypeLookup<ItemCombining>.Register(this);
			Type = NextID++;

			Mod.AddContent(new AggregatorWrapper(this));
		}
	}
}
