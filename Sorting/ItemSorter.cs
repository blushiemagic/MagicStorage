using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		public static IEnumerable<Item> SortAndFilter(IEnumerable<Item> items, SortMode sortMode, FilterMode filterMode, string modFilter, string nameFilter, int? takeCount = null)
		{
            ItemFilter filter = MakeFilter(filterMode);
			IEnumerable<Item> filteredItems = items.Where((item) => filter.Passes(item) && FilterName(item, modFilter, nameFilter));
            if (takeCount != null) filteredItems = filteredItems.Take(takeCount.Value);
		    var func = MakeSortFunction(sortMode);
		    if (func == null) return filteredItems;
            return filteredItems.OrderBy(x => x, func);
		}

		public static IEnumerable<Recipe> GetRecipes(SortMode sortMode, FilterMode filterMode, string modFilter, string nameFilter)
		{
		    var filter = MakeFilter(filterMode);
		    IEnumerable<Recipe> filteredRecipes = Main.recipe.Where((recipe, index) => index < Recipe.numRecipes && filter.Passes(recipe) && FilterName(recipe.createItem, modFilter, nameFilter));
			var func = MakeSortFunction(sortMode);
		    if (func == null) return filteredRecipes;
		    return filteredRecipes.OrderBy(x => x.createItem, func);
        }

	    static CompareFunction MakeSortFunction(SortMode sortMode)
	    {
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
	            case SortMode.Value:
	                func = new CompareValue();
	                break;
	            case SortMode.Dps:
	                func = new CompareDps();
	                break;
	            case SortMode.AsIs:
	                func = new AsIs();
	                break;
	            default:
	                func = null;
	                break;
	        }

	        return func;
	    }

	    static ItemFilter MakeFilter(FilterMode filterMode)
	    {
	        ItemFilter filter;
	        switch (filterMode)
	        {
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

	    private static bool FilterName(Item item, string modFilter, string filter)
		{
			string modName = "Terraria";
			if (item.modItem != null)
			{
				modName = item.modItem.mod.DisplayName;
			}
			return modName.ToLowerInvariant().IndexOf(modFilter.ToLowerInvariant()) >= 0 && item.Name.ToLowerInvariant().IndexOf(filter.ToLowerInvariant()) >= 0;
		}
	}
}
