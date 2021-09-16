using System;
using Terraria;

namespace MagicStorage
{
	public struct ItemData : IComparable<ItemData>
	{
		public readonly int Type;
		public readonly int Prefix;

		public ItemData(int type, int prefix = 0)
		{
			Type = type;
			Prefix = prefix;
		}

		public ItemData(Item item) : this(item.type, item.prefix)
		{
		}

		public bool Equals(ItemData other) => Type == other.Type && Prefix == other.Prefix;

		public override bool Equals(object obj) => obj is ItemData other && Equals(other);

		public override int GetHashCode() => Type * 397 + Prefix;

		public static bool operator ==(ItemData left, ItemData right) => left.Equals(right);

		public static bool operator !=(ItemData left, ItemData right) => !left.Equals(right);

		public static bool Matches(Item item1, Item item2) => new ItemData(item1) == new ItemData(item2);

		public int CompareTo(ItemData other)
		{
			int typeComparison = Type.CompareTo(other.Type);
			if (typeComparison != 0)
				return typeComparison;
			return Prefix.CompareTo(other.Prefix);
		}
	}
}
