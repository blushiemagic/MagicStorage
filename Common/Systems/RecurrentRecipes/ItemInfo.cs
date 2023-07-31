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
	}
}
