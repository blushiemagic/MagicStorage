using System;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Sorting
{
    public abstract class CompareFunction : IComparer<Item>
	{
		public abstract int Compare(Item item1, Item item2);

		public int Compare(object object1, object object2)
		{
			if (object1 is Item && object2 is Item)
			{
				return Compare((Item)object1, (Item)object2);
			}
			if (object1 is Recipe && object2 is Recipe)
			{
				return Compare(((Recipe)object1).createItem, ((Recipe)object2).createItem);
			}
			return 0;
		}
	}

	public class CompareID : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
			return item1.type - item2.type;
		}
	}

	public class CompareName : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
			return string.Compare(item1.Name, item2.Name, StringComparison.OrdinalIgnoreCase);
		}
	}

	public class CompareQuantity : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
			return (int)Math.Ceiling((float)item2.stack / (float)item2.maxStack) - (int)Math.Ceiling((float)item1.stack / (float)item1.maxStack);
		}
	}

	public class CompareValue : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
            return item1.value - item2.value;
		}
	}

	public class CompareDps : CompareFunction
	{
        public override int Compare(Item item1, Item item2)
        {
            return (int)((GetDps(item1) - GetDps(item2)) * 100);
        }

        public static float GetDps(Item item)
        {
            return item.damage / Math.Max((item.useTime + item.reuseDelay) / 60f, 0.001f);
        }
	}

	public class AsIs : CompareFunction
	{
        public override int Compare(Item item1, Item item2)
        {
            return 0;
        }
	}
}