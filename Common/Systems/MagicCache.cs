#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using SerousCommonLib.API.Helpers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems;

public class MagicCache : ModSystem
{
	public class LazyRecipe {
		public readonly int itemType;

		private readonly Lazy<Recipe[]> lazy;

		public LazyRecipe(int itemType) {
			this.itemType = itemType;

			lazy = new(() => EnabledRecipes.Where(r => r.createItem.type == this.itemType || r.requiredItem.Any(i => i.type == this.itemType)).ToArray(), isThreadSafe: false);
		}

		public Recipe[] Value => lazy.Value;
	}

	public class LazyRecipeTile {
		public readonly int tileType;

		private readonly Lazy<Recipe[]> lazy;

		public LazyRecipeTile(int tileType) {
			this.tileType = tileType;

			lazy = new(() => EnabledRecipes.Where(r => r.requiredTile.Any(t => t == this.tileType)).ToArray(), isThreadSafe: false);
		}

		public Recipe[] Value => lazy.Value;
	}

	public static Recipe[] EnabledRecipes { get; private set; } = null!;
	public static Dictionary<int, Recipe[]> ResultToRecipe { get; private set; } = null!;
	public static Dictionary<int, Recipe[]> FilteredRecipesCache { get; private set; } = null!;

	public static Dictionary<int, List<Recipe>> hasIngredient { get; private set; } = null!;
	public static Dictionary<int, List<Recipe>> hasTile { get; private set; } = null!;

	public static Mod[] AllMods { get; private set; } = null!;
	public static Dictionary<Mod, int> IndexByMod { get; private set; } = null!;
	public static Dictionary<Mod, Recipe[]> RecipesByMod { get; private set; } = null!;
	public static Recipe[] VanillaRecipes { get; private set; } = null!;

	public static Dictionary<int, LazyRecipe> RecipesUsingItemType { get; private set; } = null!;

	public static Dictionary<int, LazyRecipeTile> RecipesUsingTileType { get; private set; } = null!;

	/// <summary>
	/// Clears the dictionaries, arrays and lists for recipes and repopulates them with the current state of the <see cref="Main.recipe"/> array.<br/>
	/// Also forces the active crafting UI to refresh if applicable.
	/// </summary>
	public static void RecalculateRecipeCaches() {
		ModContent.GetInstance<MagicCache>().PostSetupRecipes();

		if (!Main.gameMenu && MagicUI.IsCraftingUIOpen())
			StorageGUI.SetRefresh(forceFullRefresh: true);

		// TODO: refresh recursive recipes
	}

	public override void Unload()
	{
		EnabledRecipes = null!;
		ResultToRecipe = null!;
		FilteredRecipesCache = null!;

		hasIngredient = null!;
		hasTile = null!;

		AllMods = null!;
		IndexByMod = null!;
		RecipesByMod = null!;
		VanillaRecipes = null!;

		RecipesUsingItemType = null!;
	}

	public override void PostSetupRecipes()
	{
		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::PostSetupRecipes");

		// PostSetupContent() is too early for name sorting, which causes the fuzzy sorting to fail
		SortingCache.dictionary.Fill();

		EnabledRecipes = Main.recipe.Take(Recipe.numRecipes).Where(r => !r.Disabled).ToArray();
		ResultToRecipe = EnabledRecipes.GroupBy(r => r.createItem.type).ToDictionary(x => x.Key, x => x.ToArray());

		hasIngredient = new();
		hasTile = new();

		//Initialize the lookup tables
		foreach (var recipe in EnabledRecipes)
		{
			foreach (var item in recipe.requiredItem)
			{
				if (!hasIngredient.TryGetValue(item.type, out var list))
					hasIngredient[item.type] = list = new();

				list.Add(recipe);
			}

			foreach (var tile in recipe.requiredTile)
			{
				if (!hasTile.TryGetValue(tile, out var list))
					hasTile[tile] = list = new();

				list.Add(recipe);
			}
		}

		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::SetupSortFilterRecipeCache");

		SetupSortFilterRecipeCache();

		var groupedByMod = EnabledRecipes.GroupBy(r => r.Mod).ToArray();

		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::RecipesByMod");

		RecipesByMod = groupedByMod.Where(x => x.Key is not null).ToDictionary(x => x.Key, x => x.ToArray());

		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::VanillaRecipes");

		VanillaRecipes = groupedByMod.Where(x => x.Key is null).SelectMany(x => x.ToArray()).ToArray();

		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::AllMods");

		// TODO: Split into mods with recipe and mods with items. Also have to account for it in ModSearchBox
		AllMods = ModLoader.Mods
			.Where(mod => RecipesByMod.ContainsKey(mod) || mod.GetContent<ModItem>().Any())
			.ToArray();

		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::IndexByMod");

		IndexByMod = AllMods
			.Select((mod, index) => (mod, index))
			.ToDictionary(x => x.mod, x => x.index);

		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::RecipesUsingItemType");

		RecipesUsingItemType = ContentSamples.ItemsByType.Where(kvp => !kvp.Value.IsAir)
			.ToDictionary(kvp => kvp.Key, kvp => new LazyRecipe(kvp.Key));

		ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicCache::RecipesUsingTileType");

		RecipesUsingTileType = Enumerable.Range(TileID.Dirt, TileLoader.TileCount)
			.ToDictionary(type => type, type => new LazyRecipeTile(type));

		ModLoadingProgressHelper.SetLoadingSubProgressText("");
	}

	private static void SetupSortFilterRecipeCache()
	{
		FilteredRecipesCache = new();

		foreach (var option in FilteringOptionLoader.Options) {
			if (option == FilteringOptionLoader.Definitions.Recent)
				continue;

			var filter = option.Filter;

			var recipes = EnabledRecipes.Where(r => filter(r.createItem));

			FilteredRecipesCache[option.Type] = recipes.ToArray();
		}
	}
}
