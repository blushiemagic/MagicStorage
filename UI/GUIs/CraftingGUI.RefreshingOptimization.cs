using MagicStorage.Common.Collections;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		private static HashSet<int> recipesToRefreshByIndex;

		private static IEnumerable<Recipe> CollectRefreshingRecipes() => recipesToRefreshByIndex is null ? [] : recipesToRefreshByIndex.Select(RecipeIndexToRecipe);

		private static int RecipeToRecipeIndex(Recipe recipe) => recipe.RecipeIndex;

		private static Recipe RecipeIndexToRecipe(int index) => Main.recipe[index];

		/// <summary>
		/// Adds <paramref name="recipes"/> to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="recipes">An array of recipes to update.  If <see langword="null"/>, then nothing happens</param>
		public static void SetNextDefaultRecipeCollectionToRefresh(Recipe[] recipes) => AddOrUpdateRefreshingRecipesCollection(recipes);

		private static void AddOrUpdateRefreshingRecipesCollection(IEnumerable<Recipe> recipes) {
			if (recipes is null)
				return;

			IEnumerable<Recipe> fullRecipeList = recipes is null
				? recipes
				: ExpandRecipeCollectionWithPossibleRecursionDependents(recipes);

			if (recipesToRefreshByIndex is null) {
				// Set the initial collection
				recipesToRefreshByIndex = [.. recipes.Select(RecipeToRecipeIndex)];
			} else {
				// Add to the existing collection
				foreach (int recipeIndex in fullRecipeList.Select(RecipeToRecipeIndex))
					recipesToRefreshByIndex.Add(recipeIndex);
			}
		}

		private static IEnumerable<Recipe> ExpandRecipeCollectionWithPossibleRecursionDependents(IEnumerable<Recipe> toRefresh) {
			if (!MagicStorageConfig.IsRecursionEnabled)
				return toRefresh;

			return toRefresh.Concat(
				toRefresh.Select(RecipeToRecipeIndex)
					.SelectManyByDictionary<int, List<Node>, Node>(MagicCache.RecursiveRecipesUsingRecipeByIndex)
					.Select(NodeToRecipe)
			);
		}

		private static Recipe NodeToRecipe(Node node) => node.info.sourceRecipe;

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedItemType"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemType">The item type to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(int affectedItemType) {
			if (MagicCache.RecipesUsingItemType.TryGetValue(affectedItemType, out var lazyResult))
				AddOrUpdateRefreshingRecipesCollection(lazyResult);
		}

		/// <summary>
		/// Adds all recipes which use the IDs in <paramref name="affectedItemTypes"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemTypes">A collection of item types to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(IEnumerable<int> affectedItemTypes) {
			AddOrUpdateRefreshingRecipesCollection(
				affectedItemTypes.SelectManyByDictionary<int, MagicCache.LazyRecipe, Recipe>(MagicCache.RecipesUsingItemType)
			);
		}

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedTileType"/> as a required tile to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileType">The tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(int affectedTileType) {
			if (MagicCache.RecipesUsingTileType.TryGetValue(affectedTileType, out var lazyResult))
				AddOrUpdateRefreshingRecipesCollection(lazyResult);
		}

		/// <summary>
		/// Adds all recipes which the IDs in <paramref name="affectedTileTypes"/> as a required tile to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileTypes">A collection of the tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(IEnumerable<int> affectedTileTypes) {
			AddOrUpdateRefreshingRecipesCollection(
				affectedTileTypes.SelectManyByDictionary<int, MagicCache.LazyRecipeTile, Recipe>(MagicCache.RecipesUsingTileType)
			);
		}
	}
}
