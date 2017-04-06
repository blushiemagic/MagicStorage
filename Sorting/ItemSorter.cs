using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		public static IEnumerable<Item> SortAndFilter(IEnumerable<Item> items, SortMode sortMode, string filter)
		{
			IEnumerable<Item> filteredItems = items.Where((item) => item.name.ToLowerInvariant().IndexOf(filter) >= 0);
			CompareFunction func;
			switch (sortMode)
			{
			case SortMode.Default:
				func = new CompareDefault();
				break;
			case SortMode.Id:
				func = new CompareID();
				break;
			case SortMode.Name:
				func = new CompareName();
				break;
			case SortMode.Quantity:
				func = new CompareQuantity();
				break;
			default:
				return filteredItems;
			}
			BTree sortedTree = new BTree(func);
			foreach (Item item in filteredItems)
			{
				sortedTree.Insert(item);
			}
			return sortedTree.GetSortedItems();
		}
	}
}