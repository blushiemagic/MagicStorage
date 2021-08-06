using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		public static IEnumerable<Item> SortAndFilter(IEnumerable<Item> items, SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter, int? takeCount = null)
		{
			ItemFilter filter = MakeFilter(filterMode);
			IEnumerable<Item> filteredItems = items.Where(item => filter.Passes(item) && FilterName(item, nameFilter) && FilterMod(item, modFilterIndex));
			if (takeCount != null)
				filteredItems = filteredItems.Take(takeCount.Value);
			CompareFunction func = MakeSortFunction(sortMode);
			return func is null ? filteredItems : filteredItems.OrderBy(x => x, func).ThenBy(x => x.type).ThenBy(x => x.value);
		}

		public static IEnumerable<Recipe> GetRecipes(SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter)
		{
			ItemFilter filter = MakeFilter(filterMode);
			IEnumerable<Recipe> filteredRecipes = Main.recipe.Where((recipe, index) => index < Recipe.numRecipes && filter.Passes(recipe) && FilterName(recipe.createItem, nameFilter) && FilterMod(recipe.createItem, modFilterIndex));
			CompareFunction func = MakeSortFunction(sortMode);
			return func is null ? filteredRecipes : filteredRecipes.OrderBy(x => x.createItem, func).ThenBy(x => x.createItem.type).ThenBy(x => x.createItem.value);
		}

		private static CompareFunction MakeSortFunction(SortMode sortMode)
		{
			CompareFunction func = sortMode switch
			{
				SortMode.Default => new CompareDefault(),
				SortMode.Id      => new CompareID(),
				SortMode.Name    => new CompareName(),
				SortMode.Value   => new CompareValue(),
				SortMode.Dps     => new CompareDps(),
				_                => null
			};

			return func;
		}

		private static ItemFilter MakeFilter(FilterMode filterMode)
		{
			ItemFilter filter = filterMode switch
			{
				FilterMode.All           => new FilterAll(),
				FilterMode.WeaponsMelee  => new FilterWeaponMelee(),
				FilterMode.WeaponsRanged => new FilterWeaponRanged(),
				FilterMode.WeaponsMagic  => new FilterWeaponMagic(),
				FilterMode.WeaponsSummon => new FilterWeaponSummon(),
				FilterMode.Ammo          => new FilterAmmo(),
				FilterMode.WeaponsThrown => new FilterWeaponThrown(),
				FilterMode.Tools         => new FilterTool(),
				FilterMode.Armor         => new FilterArmor(),
				FilterMode.Vanity        => new FilterVanity(),
				FilterMode.Equipment     => new FilterEquipment(),
				FilterMode.Potions       => new FilterPotion(),
				FilterMode.Placeables    => new FilterPlaceable(),
				FilterMode.Misc          => new FilterMisc(),
				FilterMode.Recent        => throw new NotSupportedException(),
				_                        => new FilterAll()
			};

			return filter;
		}

		private static bool FilterName(Item item, string filter)
		{
			if (filter.Trim().Length == 0)
				filter = string.Empty;
			return item.Name.ToLowerInvariant().Contains(filter.Trim().ToLowerInvariant());
		}

		private static bool FilterMod(Item item, int modFilterIndex)
		{
			if (modFilterIndex == ModSearchBox.ModIndexAll)
				return true;
			Mod[] allMods = MagicStorage.AllMods;
			int index = ModSearchBox.ModIndexBaseGame;
			if (item.ModItem != null)
				index = Array.IndexOf(allMods, item.ModItem.Mod);
			return index == modFilterIndex;
		}
	}
}
