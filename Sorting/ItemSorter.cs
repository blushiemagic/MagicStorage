using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorageExtra.Sorting
{
	public static class ItemSorter
	{
		public static IEnumerable<Item> SortAndFilter(IEnumerable<Item> items, SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter, int? takeCount = null) {
			ItemFilter filter = MakeFilter(filterMode);
			IEnumerable<Item> filteredItems = items.Where(item => filter.Passes(item) && FilterName(item, nameFilter) && FilterMod(item, modFilterIndex));
			if (takeCount != null) filteredItems = filteredItems.Take(takeCount.Value);
			CompareFunction func = MakeSortFunction(sortMode);
			return func == null ? filteredItems : filteredItems.OrderBy(x => x, func).ThenBy(x => x.type).ThenBy(x => x.value);
		}

		public static IEnumerable<Recipe> GetRecipes(SortMode sortMode, FilterMode filterMode, int modFilterIndex, string nameFilter) {
			ItemFilter filter = MakeFilter(filterMode);
			Recipe[] recipes = Main.recipe;
			if (RecursiveCraftIntegration.Enabled)
				RecursiveCraftIntegration.RecursiveRecipes();
			IEnumerable<Recipe> filteredRecipes = recipes.Where((recipe, index) => index < Recipe.numRecipes && filter.Passes(recipe) && FilterName(recipe.createItem, nameFilter) && FilterMod(recipe.createItem, modFilterIndex));
			CompareFunction func = MakeSortFunction(sortMode);
			return func == null ? filteredRecipes : filteredRecipes.OrderBy(x => x.createItem, func).ThenBy(x => x.createItem.type).ThenBy(x => x.createItem.value);
		}

		private static CompareFunction MakeSortFunction(SortMode sortMode) {
			CompareFunction func;
			switch (sortMode) {
				case SortMode.Default:
					func = new CompareDefault();
					break;
				case SortMode.Id:
					func = new CompareID();
					break;
				case SortMode.Name:
					func = new CompareName();
					break;
				case SortMode.Value:
					func = new CompareValue();
					break;
				case SortMode.Dps:
					func = new CompareDps();
					break;
				default:
					func = null;
					break;
			}

			return func;
		}

		private static ItemFilter MakeFilter(FilterMode filterMode) {
			ItemFilter filter;
			switch (filterMode) {
				case FilterMode.All:
					filter = new FilterAll();
					break;
				case FilterMode.WeaponsMelee:
					filter = new FilterWeaponMelee();
					break;
				case FilterMode.WeaponsRanged:
					filter = new FilterWeaponRanged();
					break;
				case FilterMode.WeaponsMagic:
					filter = new FilterWeaponMagic();
					break;
				case FilterMode.WeaponsSummon:
					filter = new FilterWeaponSummon();
					break;
				case FilterMode.Ammo:
					filter = new FilterAmmo();
					break;
				case FilterMode.WeaponsThrown:
					filter = new FilterWeaponThrown();
					break;
				case FilterMode.Tools:
					filter = new FilterTool();
					break;
				case FilterMode.Armor:
					filter = new FilterArmor();
					break;
				case FilterMode.Vanity:
					filter = new FilterVanity();
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
				case FilterMode.Recent:
					throw new NotSupportedException();
				default:
					filter = new FilterAll();
					break;
			}

			return filter;
		}


		private static bool FilterName(Item item, string filter) {
			if (filter.Trim().Length == 0) filter = string.Empty;
			return item.Name.ToLowerInvariant().IndexOf(filter.Trim().ToLowerInvariant(), StringComparison.Ordinal) >= 0;
		}

		private static bool FilterMod(Item item, int modFilterIndex) {
			if (modFilterIndex == ModSearchBox.ModIndexAll) return true;
			Mod[] allMods = MagicStorageExtra.Instance.AllMods;
			int index = ModSearchBox.ModIndexBaseGame;
			if (item.modItem != null)
				index = Array.IndexOf(allMods, item.modItem.mod);
			return index == modFilterIndex;
		}
	}
}
