using System;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public readonly struct ItemInfo {
		public readonly int type;
		public readonly int stack;
		public readonly int prefix;  // Used when converting to ItemData

		public ItemInfo(int type, int stack, int prefix = 0) {
			this.type = type;
			this.stack = stack;
			this.prefix = prefix;
		}

		public ItemInfo(Item item) : this(item.type, item.stack, item.prefix) { }

		public ItemInfo UpdateStack(int add) => new ItemInfo(type, stack + add, prefix);

		public ItemInfo SetStack(int newStack) => new ItemInfo(type, newStack, prefix);

		public override bool Equals(object obj) {
			return obj is ItemInfo info && type == info.type && stack == info.stack && prefix == info.prefix;
		}

		public bool EqualsIgnoreStack(ItemInfo other) {
			return type == other.type && prefix == other.prefix;
		}

		public override int GetHashCode() {
			return HashCode.Combine(type, stack, prefix);
		}

		public static bool operator ==(ItemInfo left, ItemInfo right) {
			return left.type == right.type && left.stack == right.stack && left.prefix == right.prefix;
		}

		public static bool operator !=(ItemInfo left, ItemInfo right) {
			return !(left == right);
		}
	}
}
