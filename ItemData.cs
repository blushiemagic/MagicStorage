using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage
{
    public struct ItemData
    {
        public readonly int Type;
        public readonly int Prefix;

        public ItemData(int type, int prefix = 0)
        {
            this.Type = type;
            this.Prefix = prefix;
        }

        public ItemData(Item item)
        {
            this.Type = item.netID;
            this.Prefix = item.prefix;
        }

        public override bool Equals(Object other)
        {
            if (!(other is ItemData))
            {
                return false;
            }
            return Matches(this, (ItemData)other);
        }

        public override int GetHashCode()
        {
            return 100 * Type + Prefix;
        }

        public static bool Matches(Item item1, Item item2)
        {
            return Matches(new ItemData(item1), new ItemData(item2));
        }

        public static bool Matches(ItemData data1, ItemData data2)
        {
            return data1.Type == data2.Type && data1.Prefix == data2.Prefix;
        }

        public static int Compare(Item item1, Item item2)
        {
            ItemData data1 = new ItemData(item1);
            ItemData data2 = new ItemData(item2);
            if (data1.Type != data2.Type)
            {
                return data1.Type - data2.Type;
            }
            return data1.Prefix - data2.Prefix;
        }

		public static bool operator ==(ItemData left, ItemData right) {
			return left.Equals(right);
		}

		public static bool operator !=(ItemData left, ItemData right) {
			return !(left == right);
		}
	}
}