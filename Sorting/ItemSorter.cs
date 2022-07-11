using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorage.Common.Systems;
using Terraria;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		public static IEnumerable<Item> SortAndFilter(
			IEnumerable<Item> items, SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter, int? takeCount = null)
		{
			var filter = GetFilter(filterMode);
			IEnumerable<Item> filteredItems = items.Where(item => filter(item) && FilterName(item, nameFilter) && FilterMod(item, modFilterIndex));
			if (takeCount is not null)
				filteredItems = filteredItems.Take(takeCount.Value);

			filteredItems = Aggregate(filteredItems);

			if (sortMode == SortMode.AsIs)
				return filteredItems;

			//Apply "fuzzy" sorting since it's faster, but less accurate
			IOrderedEnumerable<Item> orderedItems = SortingCache.dictionary.SortFuzzy(filteredItems, sortMode);

			if (sortMode == SortMode.Value) {
				//Ignore sorting by type
				return orderedItems.ThenBy(x => x.value);
			} else if (sortMode == SortMode.Dps) {
				//Sort again by DPS due to it using variable item data
				var func = GetSortFunction(SortMode.Dps);
				return orderedItems.ThenBy(x => x, func).ThenBy(x => x.value);
			}

			return orderedItems.ThenBy(x => x.type).ThenBy(x => x.value);
		}

		public static IEnumerable<Item> Aggregate(IEnumerable<Item> items)
		{
			Item lastItem = null;

			foreach (Item item in items.OrderBy(i => i.type))
			{
				if (lastItem is null)
				{
					lastItem = item.Clone();
					continue;
				}

				if (ItemCombining.CanCombineItems(item, lastItem) && lastItem.stack + item.stack > 0)
				{
					lastItem.stack += item.stack;
				}
				else
				{
					yield return lastItem;
					lastItem = item.Clone();
				}
			}

			if (lastItem is not null)
				yield return lastItem;
		}

		public static ParallelQuery<Recipe> GetRecipes(SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter, out IComparer<Item> sortComparer)
		{
			sortComparer = GetSortFunction(sortMode);
			return MagicCache.FilteredRecipesCache[filterMode]
				.AsParallel()
				.AsOrdered()
				.Where(recipe => FilterName(recipe.createItem, nameFilter) && FilterMod(recipe.createItem, modFilterIndex));
		}

		internal static IComparer<Item> GetSortFunction(SortMode sortMode)
		{
			return sortMode switch
			{
				SortMode.Default => CompareDefault.Instance,
				SortMode.Id      => CompareID.Instance,
				SortMode.Name    => CompareName.Instance,
				SortMode.Value   => CompareValue.Instance,
				SortMode.Dps     => CompareDps.Instance,
				_                => null,
			};
		}

		internal static ItemFilter.Filter GetFilter(FilterMode filterMode)
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

		internal static bool FilterName(Item item, string filter) => item.Name.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

		internal static bool FilterMod(Item item, int modFilterIndex)
		{
			if (modFilterIndex == ModSearchBox.ModIndexAll)
				return true;

			int index = ModSearchBox.ModIndexBaseGame;

			if (item.ModItem is not null)
				index = MagicCache.IndexByMod[item.ModItem.Mod];

			return index == modFilterIndex;
		}
	}
}
