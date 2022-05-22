using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		public static IEnumerable<Item> SortAndFilter(IEnumerable<Item> items, SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter,
			int? takeCount = null)
		{
			var filter = MakeFilter(filterMode);
			IEnumerable<Item> filteredItems = items.Where(item => filter(item) && FilterName(item, nameFilter) && FilterMod(item, modFilterIndex));
			if (takeCount is not null)
				filteredItems = filteredItems.Take(takeCount.Value);

			filteredItems = Aggregate(filteredItems);

			CompareFunction func = MakeSortFunction(sortMode);
			return func is null ? filteredItems : filteredItems.OrderBy(x => x, func).ThenBy(x => x.type).ThenBy(x => x.value);
		}

		public static IEnumerable<Item> Aggregate(IEnumerable<Item> items)
		{
			Dictionary<ItemData, Item> dict = new();

			foreach (Item item in items)
			{
				ItemData itemData = new(item);
				if (dict.TryGetValue(itemData, out Item i))
					i.stack += item.stack;
				else
					dict.Add(itemData, item.Clone());
			}

			return dict.Values;
		}

		public static ParallelQuery<Recipe> GetRecipes(SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter)
		{
			var filter = MakeFilter(filterMode);
			var filteredRecipes = Main.recipe
				.AsParallel()
				.AsOrdered()
				.Take(Recipe.numRecipes)
				.Where(recipe => filter(recipe.createItem) && FilterName(recipe.createItem, nameFilter) && FilterMod(recipe.createItem, modFilterIndex));

			CompareFunction sortFunction = MakeSortFunction(sortMode);
			return sortFunction is null
				? filteredRecipes
				: filteredRecipes.OrderBy(x => x.createItem, sortFunction).ThenBy(x => x.createItem.type).ThenBy(x => x.createItem.value);
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
				_                => null,
			};

			return func;
		}

		private static ItemFilter.Filter MakeFilter(FilterMode filterMode)
		{
			return filterMode switch
			{
				FilterMode.All           => ItemFilter.All,
				FilterMode.WeaponsMelee  => MagicStorageConfig.ExtraFilterIcons ? ItemFilter.WeaponMelee : ItemFilter.Weapon,
				FilterMode.WeaponsRanged => ItemFilter.WeaponRanged,
				FilterMode.WeaponsMagic  => ItemFilter.WeaponMagic,
				FilterMode.WeaponsSummon => ItemFilter.WeaponSummon,
				FilterMode.Ammo          => ItemFilter.Ammo,
				FilterMode.WeaponsThrown => ItemFilter.WeaponThrown,
				FilterMode.Tools         => ItemFilter.Tool,
				FilterMode.Armor         => ItemFilter.Armor,
				FilterMode.Vanity        => ItemFilter.Vanity,
				FilterMode.Equipment     => ItemFilter.Equipment,
				FilterMode.Potions       => ItemFilter.Potion,
				FilterMode.Placeables    => ItemFilter.Placeable,
				FilterMode.Misc          => ItemFilter.Misc,
				FilterMode.Recent        => throw new NotSupportedException(),
				_                        => ItemFilter.All,
			};
		}

		private static bool FilterName(Item item, string filter) => item.Name.ToLowerInvariant().Contains(filter.Trim().ToLowerInvariant());

		private static bool FilterMod(Item item, int modFilterIndex)
		{
			if (modFilterIndex == ModSearchBox.ModIndexAll)
				return true;

			int index = ModSearchBox.ModIndexBaseGame;

			if (item.ModItem is not null)
				index = MagicStorage.IndexByMod[item.ModItem.Mod];

			return index == modFilterIndex;
		}
	}
}
