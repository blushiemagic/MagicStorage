using System;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Sorting
{
	public abstract class CompareFunction
	{
		public abstract int Compare(Item item1, Item item2);
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
}