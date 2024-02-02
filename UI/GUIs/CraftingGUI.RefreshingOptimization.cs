using MagicStorage.Common.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		private static Recipe[] recipesToRefresh;

		/// <summary>
		/// Adds <paramref name="recipes"/> to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="recipes">An array of recipes to update.  If <see langword="null"/>, then nothing happens</param>
		public static void SetNextDefaultRecipeCollectionToRefresh(Recipe[] recipes) {
			if (recipesToRefresh is null) {
				if (recipes is not null) {
					recipes = ExpandRecipeCollectionWithPossibleRecursionDependents(recipes).ToArray();

					NetHelper.Report(true, $"Setting next refresh to check {recipes.Length} recipes");
				}

				recipesToRefresh = recipes;
				return;
			}

			if (recipes is null)
				return;

			var updatedList = recipesToRefresh.Concat(recipes);
			updatedList = ExpandRecipeCollectionWithPossibleRecursionDependents(updatedList);

			recipesToRefresh = updatedList.DistinctBy(static r => r, ReferenceEqualityComparer.Instance).ToArray();

			NetHelper.Report(true, $"Setting next refresh to check {recipesToRefresh.Length} recipes");
		}

		private static IEnumerable<Recipe> ExpandRecipeCollectionWithPossibleRecursionDependents(IEnumerable<Recipe> toRefresh) {
			if (!MagicStorageConfig.IsRecursionEnabled)
				return toRefresh;

			return toRefresh.Concat(toRefresh.SelectMany(static r => MagicCache.RecursiveRecipesUsingRecipeByIndex.TryGetValue(r.RecipeIndex, out var recipes)
				? recipes.Select(static node => node.info.sourceRecipe)
				: Array.Empty<Recipe>()));
		}

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedItemType"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemType">The item type to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(int affectedItemType) {
			SetNextDefaultRecipeCollectionToRefresh(MagicCache.RecipesUsingItemType.TryGetValue(affectedItemType, out var result) ? result.Value : null);
		}

		/// <summary>
		/// Adds all recipes which use the IDs in <paramref name="affectedItemTypes"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemTypes">A collection of item types to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(IEnumerable<int> affectedItemTypes) {
			SetNextDefaultRecipeCollectionToRefresh(affectedItemTypes.SelectMany(static i => MagicCache.RecipesUsingItemType.TryGetValue(i, out var result) ? result.Value : Array.Empty<Recipe>())
				.DistinctBy(static r => r, ReferenceEqualityComparer.Instance)
				.ToArray());
		}

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedTileType"/> as a required tile to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileType">The tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(int affectedTileType) {
			SetNextDefaultRecipeCollectionToRefresh(MagicCache.RecipesUsingTileType.TryGetValue(affectedTileType, out var result) ? result.Value : null);
		}

		/// <summary>
		/// Adds all recipes which the IDs in <paramref name="affectedTileTypes"/> as a required tile to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileTypes">A collection of the tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(IEnumerable<int> affectedTileTypes) {
			SetNextDefaultRecipeCollectionToRefresh(affectedTileTypes.SelectMany(static t => MagicCache.RecipesUsingTileType.TryGetValue(t, out var result) ? result.Value : Array.Empty<Recipe>())
				.DistinctBy(static r => r, ReferenceEqualityComparer.Instance)
				.ToArray());
		}
	}
}
