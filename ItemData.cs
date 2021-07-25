using Terraria;

namespace MagicStorage
{
	public struct ItemData
	{
		public readonly int Type;
		public readonly int Prefix;

		public ItemData(int type, int prefix = 0)
		{
			Type = type;
			Prefix = prefix;
		}

		public ItemData(Item item)
		{
			Type = item.type;
			Prefix = item.prefix;
		}

		public override bool Equals(object other) => other is ItemData data && Matches(this, data);

		public override int GetHashCode() => 100 * Type + Prefix;

		public static bool Matches(Item item1, Item item2) => Matches(new ItemData(item1), new ItemData(item2));

		public static bool Matches(ItemData data1, ItemData data2) => data1.Type == data2.Type && data1.Prefix == data2.Prefix;

		public static int Compare(Item item1, Item item2)
		{
			var data1 = new ItemData(item1);
			var data2 = new ItemData(item2);
			if (data1.Type != data2.Type)
				return data1.Type - data2.Type;
			return data1.Prefix - data2.Prefix;
		}
	}
}
