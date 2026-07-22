using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	/// <summary>
	/// Represents the filters, sorting and other controls used to control whether and how items/recipes are displayed in the UIs for this mod.
	/// </summary>
	public class StorageViewControls {
		/// <summary>
		/// The selected <see cref="SortingOption"/>
		/// </summary>
		public readonly int sortingOption;

		/// <summary>
		/// The selected <see cref="FilteringOption"/>
		/// </summary>
		public readonly int filteringOption;
		private readonly HashSet<int> _generalFilters;

		/// <summary>
		/// The full search text entered by the user.<br/>
		/// This may contain special prefixes such as '@' or '#' to indicate mod searches or tooltip searches respectively.<br/>
		/// The other search text fields are derived from this field.
		/// </summary>
		public readonly string fullSearchText;
		/// <summary>
		/// The mod search text without the preceding <c>@</c> prefix, if any; otherwise, <see langword="null"/>.<br/>
		/// If <see cref="modSearchOption"/> is not set to <see cref="ModSearchBox.ModIndexAll"/>, this field will always be <see langword="null"/>.
		/// <para/>
		/// Examples:<br/>
		/// <list type="bullet">
		/// <item>Full text: <c>@MagicStorage #storing items</c></item>
		/// <item>Mod search: <c>MagicStorage</c></item>
		/// </list>
		/// <list type="bullet">
		/// <item>Full text: <c>@ModLoader Unloaded Item</c></item>
		/// <item>Mod search: <c>ModLoader</c></item>
		/// </list>
		/// </summary>
		public readonly string modSearchText;
		/// <summary>
		/// The item name search text, if any; otherwise, <see langword="null"/>.
		/// <para/>
		/// Examples:<br/>
		/// <list type="bullet">
		/// <item>Full text: <c>@MagicStorage #storing items</c></item>
		/// <item>Name search: <see langword="null"/></item>
		/// </list>
		/// <list type="bullet">
		/// <item>Full text: <c>@ModLoader Unloaded Item</c></item>
		/// <item>Name search: <c>Unloaded Item</c></item>
		/// </list>
		/// </summary>
		public readonly string itemNameSearchText;
		/// <summary>
		/// The item tooltip search text without the preceding <c>#</c> prefix, if any; otherwise, <see langword="null"/>.
		/// <para/>
		/// Examples:<br/>
		/// <list type="bullet">
		/// <item>Full text: <c>@MagicStorage #storing items</c></item>
		/// <item>Tooltip search: <c>storing items</c></item>
		/// </list>
		/// <list type="bullet">
		/// <item>Full text: <c>@ModLoader Unloaded Item</c></item>
		/// <item>Tooltip search: <see langword="null"/></item>
		/// </list>
		/// </summary>
		public readonly string itemTooltipSearchText;

		/// <summary>
		/// Whether only favorited items/recipes will appear
		/// </summary>
		public readonly bool showOnlyFavorites;

		/// <summary>
		/// The current option for the "mod search" button, typically located underneath the search prompt.
		/// </summary>
		public readonly int modSearchOption;

		/// <summary>
		/// Whether any <see cref="FilteringOption"/> where <see cref="FilteringOption.IsGeneralFilter"/><c> == <see langword="true"/></c> is active.
		/// </summary>
		public bool HasGeneralFilters => _generalFilters.Count > 0;

		/// <summary>
		/// Creates a new instance of <see cref="StorageViewControls"/> with the specified parameters.
		/// </summary>
		/// <param name="sortingOption">The selected <see cref="SortingOption"/></param>
		/// <param name="filteringOption">The selected <see cref="FilteringOption"/></param>
		/// <param name="generalFilters">The set of active <see cref="FilteringOption"/> where <see cref="FilteringOption.IsGeneralFilter"/><c> == <see langword="true"/></c></param>
		/// <param name="fullSearchText">The full search text entered by the user</param>
		/// <param name="showOnlyFavorites">Whether only favorited items/recipes will appear</param>
		/// <param name="modSearchOption">The current option for the "mod search" button, typically located underneath the search prompt</param>
		public StorageViewControls(
			int sortingOption,
			int filteringOption,
			IEnumerable<int> generalFilters,
			string fullSearchText,
			bool showOnlyFavorites,
			int modSearchOption
		) {
			this.sortingOption = sortingOption;
			
			this.filteringOption = filteringOption;
			_generalFilters = generalFilters is null || (generalFilters.TryGetNonEnumeratedCount(out int count) && count == 0)
				? []
				: [.. generalFilters];
			
			if (!string.IsNullOrEmpty(fullSearchText)) {
				this.fullSearchText = fullSearchText;

				AssignSearchVariables(
					ref fullSearchText,
					ref modSearchText,
					ref itemNameSearchText,
					ref itemTooltipSearchText,
					allowModSearch: modSearchOption == ModSearchBox.ModIndexAll
				);
			} else
				this.fullSearchText = string.Empty;

			this.showOnlyFavorites = showOnlyFavorites;
			this.modSearchOption = modSearchOption;
		}

		private StorageViewControls(
			int sortingOption,
			int filteringOption,
			IEnumerable<int> generalFilters,
			string fullSearchText,
			string modSearchText,
			string itemNameSearchText,
			string itemTooltipSearchText,
			bool showOnlyFavorites,
			int modSearchOption
		) {
			this.sortingOption = sortingOption;
			this.filteringOption = filteringOption;
			_generalFilters = generalFilters is null ? [] : [.. generalFilters];
			this.fullSearchText = fullSearchText;
			this.modSearchText = modSearchText;
			this.itemNameSearchText = itemNameSearchText;
			this.itemTooltipSearchText = itemTooltipSearchText;
			this.showOnlyFavorites = showOnlyFavorites;
			this.modSearchOption = modSearchOption;
		}

		/// <summary>
		/// Creates a copy of this <see cref="StorageViewControls"/> instance.
		/// </summary>
		public StorageViewControls CreateCopy() {
			return new StorageViewControls(
				sortingOption,
				filteringOption,
				_generalFilters,
				fullSearchText,
				modSearchText,
				itemNameSearchText,
				itemTooltipSearchText,
				showOnlyFavorites,
				modSearchOption
			);
		}

		/// <summary>
		/// Creates a copy of this <see cref="StorageViewControls"/> instance, overriding any specified parameters.
		/// </summary>
		/// <param name="sortingOptionOverride">If specified, overrides the selected <see cref="SortingOption"/></param>
		/// <param name="filteringOptionOverride">If specified, overrides the selected <see cref="FilteringOption"/></param>
		/// <param name="generalFiltersOverride">If specified, overrides the set of active <see cref="FilteringOption"/> where <see cref="FilteringOption.IsGeneralFilter"/><c> == <see langword="true"/></c></param>
		/// <param name="fullSearchTextOverride">If specified, overrides the full search text entered by the user</param>
		/// <param name="showOnlyFavoritesOverride">If specified, overrides whether only favorited items/recipes will appear</param>
		/// <param name="modSearchOptionOverride">If specified, overrides the current option for the "mod search" button, typically located underneath the search prompt</param>
		public StorageViewControls CreateCopy(
			int? sortingOptionOverride = null,
			int? filteringOptionOverride = null,
			HashSet<int> generalFiltersOverride = null,
			string fullSearchTextOverride = null,
			bool? showOnlyFavoritesOverride = null,
			int? modSearchOptionOverride = null
		) {
			if (fullSearchTextOverride is null) {
				// No extra logic has to run; just copy the variables
				return new StorageViewControls(
					sortingOptionOverride ?? sortingOption,
					filteringOptionOverride ?? filteringOption,
					generalFiltersOverride ?? _generalFilters,
					fullSearchText,
					modSearchText,
					itemNameSearchText,
					itemTooltipSearchText,
					showOnlyFavoritesOverride ?? showOnlyFavorites,
					modSearchOptionOverride ?? modSearchOption
				);
			} else {
				// Use the standard constructor
				return new StorageViewControls(
					sortingOptionOverride ?? sortingOption,
					filteringOptionOverride ?? filteringOption,
					generalFiltersOverride ?? _generalFilters,
					fullSearchTextOverride,
					showOnlyFavoritesOverride ?? showOnlyFavorites,
					modSearchOptionOverride ?? modSearchOption
				);
			}
		}

		private static void AssignSearchVariables(ref string remainingText, ref string modSearch, ref string nameSearch, ref string tooltipSearch, bool allowModSearch) {
			// Splice out the mod search and tooltip search text, if any
			if (remainingText.StartsWith('#')) {
				// The text is an item tooltip search only
				tooltipSearch = remainingText[1..];
				remainingText = string.Empty;
			} else if (allowModSearch && remainingText.StartsWith('@')) {
				// The text starts with a mod search
				int endOfModSearch = remainingText.IndexOf(' ');

				if (endOfModSearch < 0) {
					// The text is a mod search only
					modSearch = remainingText[1..];
					remainingText = string.Empty;
				} else {
					// The first "word" is the mod search
					modSearch = remainingText[1..endOfModSearch];
					remainingText = remainingText[(endOfModSearch + 1)..];

					// An item name or item tooltip search can follow, so check for them
					AssignSearchVariables(ref remainingText, ref modSearch, ref nameSearch, ref tooltipSearch, allowModSearch: false);
				}
			} else if (!string.IsNullOrEmpty(remainingText)) {
				// The text is an item name search
				nameSearch = remainingText;
			}
		}

		/// <summary>
		/// Returns whether any <see cref="FilteringOption"/> where <see cref="FilteringOption.IsGeneralFilter"/><c> == <see langword="true"/></c> with the specified ID is active.
		/// </summary>
		/// <param name="filterOption">The ID of the filter to check</param>
		public bool HasGeneralFilter(int filterOption) => _generalFilters.Contains(filterOption);

		/// <summary>
		/// Returns whether an item with the specified type passes all filters derived from the parameters on this object.
		/// </summary>
		/// <param name="itemType">The type of the item to check</param>
		public bool ItemPassesFilters(int itemType) => ItemPassesFilters(Utility.GetItemSample(itemType));

		/// <summary>
		/// Returns whether the specified item passes all filters derived from the parameters on this object.
		/// </summary>
		/// <param name="item">The item to check</param>
		public bool ItemPassesFilters(Item item) => ItemPassesOptionFilters(item) && ItemPassesTextFilter(item);

		/// <summary>
		/// Returns whether a recipe with the specified output item passes all filters derived from the parameters on this object.
		/// </summary>
		/// <param name="recipe">The recipe to check</param>
		public bool RecipePassesFilters(Recipe recipe) => ItemPassesFilters(recipe.createItem);

		/// <summary>
		/// Returns whether the specified item passes all <see cref="FilteringOption"/> filters on this object.
		/// </summary>
		/// <param name="item">The item to check</param>
		public bool ItemPassesOptionFilters(Item item) => ItemPassesStaticOptionFilter(item) && ItemPassesGeneralOptionFilters(item);

		/// <summary>
		/// Returns whether the specified item passes the static <see cref="FilteringOption"/> (<see cref="FilteringOption.IsGeneralFilter"/><c> == <see langword="false"/></c>) filter on this object.
		/// </summary>
		/// <param name="item">The item to check</param>
		public bool ItemPassesStaticOptionFilter(Item item) {
			if (item is not { IsAir: false })
				return false;

			if (FilteringOptionLoader.Get(filteringOption) is FilteringOption { Visible: true } option) {
				if (!option.Filter(item))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Returns whether the specified item passes all general <see cref="FilteringOption"/> (<see cref="FilteringOption.IsGeneralFilter"/><c> == <see langword="true"/></c>) filters on this object.
		/// </summary>
		/// <param name="item">The item to check</param>
		public bool ItemPassesGeneralOptionFilters(Item item) {
			if (item is not { IsAir: false })
				return false;

			foreach (int id in _generalFilters) {
				if (FilteringOptionLoader.Get(id) is FilteringOption { Visible: true, Filter: { } filterFunc } && !filterFunc(item))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Returns whether the specified item passes the search prompt and mod selection filters, if applicable.
		/// </summary>
		/// <param name="item">The item to check</param>
		public bool ItemPassesTextFilter(Item item) {
			if (item is not { IsAir: false })
				return false;

			if (modSearchOption != ModSearchBox.ModIndexAll) {
				if (modSearchOption == ModSearchBox.ModIndexBaseGame) {
					// Terraria items
					if (item.ModItem is not null)
						return false;
				} else if ((uint)modSearchOption >= (uint)MagicCache.AllMods.Length) {
					// Treat stale or corrupted mod indexes as "All Mods" instead of hiding modded content.
				} else {
					// Modded items
					if (item.ModItem is null || !object.ReferenceEquals(item.ModItem.Mod, MagicCache.AllMods[modSearchOption]))
						return false;
				}
			}

			if (string.IsNullOrEmpty(fullSearchText))
				return true;

			if (modSearchOption == ModSearchBox.ModIndexAll && !string.IsNullOrEmpty(modSearchText)) {
				string expectedMod = item.ModItem?.Mod.Name ?? "Terraria";
				if (!expectedMod.Contains(modSearchText, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (!string.IsNullOrEmpty(itemNameSearchText)) {
				if (!item.Name.Contains(itemNameSearchText, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (!string.IsNullOrEmpty(itemTooltipSearchText)) {
				try {
					// Local capturing
					string s = itemTooltipSearchText;
					if (!Utility.GetItemTooltipLines(item).Any(line => line.Contains(s, StringComparison.OrdinalIgnoreCase)))
						return false;
				} catch {
					return false;
				}
			}

			return true;
		}
	}
}
