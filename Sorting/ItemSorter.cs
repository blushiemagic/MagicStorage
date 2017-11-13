using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		public static IEnumerable<Item> SortAndFilter(IEnumerable<Item> items, SortMode sortMode, FilterMode filterMode, string modFilter, string nameFilter)
		{
			ItemFilter filter;
			switch (filterMode)
			{
			case FilterMode.All:
				filter = new FilterAll();
				break;
			case FilterMode.Weapons:
				filter = new FilterWeapon();
				break;
			case FilterMode.Tools:
				filter = new FilterTool();
				break;
			case FilterMode.Equipment:
				filter = new FilterEquipment();
				break;
			case FilterMode.Potions:
				filter = new FilterPotion();
				break;
			case FilterMode.Placeables:
				filter = new FilterPlaceable();
				break;
			case FilterMode.Misc:
				filter = new FilterMisc();
				break;
			default:
				filter = new FilterAll();
				break;
			}
			IEnumerable<Item> filteredItems = items.Where((item) => filter.Passes(item) && FilterName(item, modFilter, nameFilter));
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
				func = new CompareID();
				break;
			default:
				return filteredItems;
			}
			BTree<Item> sortedTree = new BTree<Item>(func);
			foreach (Item item in filteredItems)
			{
				sortedTree.Insert(item);
			}
			if (sortMode == SortMode.Quantity)
			{
				BTree<Item> oldTree = sortedTree;
				sortedTree = new BTree<Item>(new CompareQuantity());
				foreach (Item item in oldTree.GetSortedItems())
				{
					sortedTree.Insert(item);
				}
			}
			return sortedTree.GetSortedItems();
		}

		public static IEnumerable<Recipe> GetRecipes(SortMode sortMode, FilterMode filterMode, string modFilter, string nameFilter)
		{
			ItemFilter filter;
			switch (filterMode)
			{
			case FilterMode.All:
				filter = new FilterAll();
				break;
			case FilterMode.Weapons:
				filter = new FilterWeapon();
				break;
			case FilterMode.Tools:
				filter = new FilterTool();
				break;
			case FilterMode.Equipment:
				filter = new FilterEquipment();
				break;
			case FilterMode.Potions:
				filter = new FilterPotion();
				break;
			case FilterMode.Placeables:
				filter = new FilterPlaceable();
				break;
			case FilterMode.Misc:
				filter = new FilterMisc();
				break;
			default:
				filter = new FilterAll();
				break;
			}
			IEnumerable<Recipe> filteredRecipes = Main.recipe.Where((recipe, index) => index < Recipe.numRecipes && filter.Passes(recipe) && FilterName(recipe.createItem, modFilter, nameFilter));
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
			default:
				return filteredRecipes;
			}
			BTree<Recipe> sortedTree = new BTree<Recipe>(func);
			foreach (Recipe recipe in filteredRecipes)
			{
				sortedTree.Insert(recipe);
				if (CraftingGUI.threadNeedsRestart)
				{
					return new List<Recipe>();
				}
			}
			return sortedTree.GetSortedItems();
		}

		private static bool FilterName(Item item, string modFilter, string filter)
		{
			string modName = "Terraria";
			if (item.modItem != null)
			{
				modName = item.modItem.mod.DisplayName;
			}
			return modName.ToLowerInvariant().IndexOf(modFilter) >= 0 && item.Name.ToLowerInvariant().IndexOf(filter.ToLowerInvariant()) >= 0;
		}
	}
}