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
			return string.Compare(item1.name, item2.name, StringComparison.OrdinalIgnoreCase);
		}
	}

	public class CompareQuantity : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
			return item2.stack - item1.stack;
		}
	}
}