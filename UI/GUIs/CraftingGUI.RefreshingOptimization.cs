using MagicStorage.Common.Collections;
using MagicStorage.Common.Systems;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class CraftingGUI {
		private static HashSet<int> recipesToRefreshByIndex;
		private static HashSet<int> itemTypesToRefresh;
		private static bool forceNextRecipeRefreshToBeFull;
		private static bool refreshInventorySnapshotForNextRecipeRefresh;
		private static bool refreshSelectedRecipeSnapshotForNextRecipeRefresh;
		private const int UnboundedRecursionRefreshDepth = int.MaxValue;

		private static IEnumerable<Recipe> CollectRefreshingRecipes(out bool fullRecipeRefresh) {
			if (forceNextRecipeRefreshToBeFull || recipesToRefreshByIndex is null) {
				forceNextRecipeRefreshToBeFull = false;
				recipesToRefreshByIndex = null;
				itemTypesToRefresh = null;
				fullRecipeRefresh = true;
				return [];
			}

			Recipe[] recipes = [.. recipesToRefreshByIndex
				.Where(static index => (uint)index < (uint)Main.recipe.Length)
				.Select(RecipeIndexToRecipe)
				.Where(static recipe => recipe is not null && !recipe.Disabled)];

			recipesToRefreshByIndex = null;
			fullRecipeRefresh = false;
			return recipes;
		}

		private static HashSet<int> ConsumeItemTypesForNextRecipeRefresh() {
			HashSet<int> itemTypes = itemTypesToRefresh;
			itemTypesToRefresh = null;
			return itemTypes;
		}

		private static bool RecipeUsesAnyItemType(Recipe recipe, HashSet<int> itemTypes) {
			if (recipe is null || itemTypes is null || itemTypes.Count <= 0)
				return false;

			if (itemTypes.Contains(recipe.createItem.type))
				return true;

			foreach (Item item in recipe.requiredItem) {
				if (itemTypes.Contains(item.type))
					return true;

				foreach (int itemType in itemTypes) {
					if (RecipeGroupMatch(recipe, itemType, item.type))
						return true;
				}
			}

			return false;
		}

		private static bool ConsumeRefreshInventorySnapshotForNextRecipeRefresh() {
			bool refreshInventorySnapshot = refreshInventorySnapshotForNextRecipeRefresh;
			refreshInventorySnapshotForNextRecipeRefresh = false;
			return refreshInventorySnapshot;
		}

		private static bool ConsumeRefreshSelectedRecipeSnapshotForNextRecipeRefresh() {
			bool refreshSelectedRecipeSnapshot = refreshSelectedRecipeSnapshotForNextRecipeRefresh;
			refreshSelectedRecipeSnapshotForNextRecipeRefresh = false;
			return refreshSelectedRecipeSnapshot;
		}

		private static void ClearRecipeRefreshOptimizationState() {
			recipesToRefreshByIndex = null;
			itemTypesToRefresh = null;
			forceNextRecipeRefreshToBeFull = false;
			refreshInventorySnapshotForNextRecipeRefresh = false;
			refreshSelectedRecipeSnapshotForNextRecipeRefresh = false;
		}

		internal static void RequestSelectedRecipeSnapshotForNextRecipeRefresh() {
			refreshSelectedRecipeSnapshotForNextRecipeRefresh = true;
			refreshInventorySnapshotForNextRecipeRefresh = true;
		}

		internal static void ForceNextRecipeRefreshToBeFull() {
			forceNextRecipeRefreshToBeFull = true;
			recipesToRefreshByIndex = null;
			itemTypesToRefresh = null;
			refreshInventorySnapshotForNextRecipeRefresh = true;
			refreshSelectedRecipeSnapshotForNextRecipeRefresh = true;
		}

		private static int RecipeToRecipeIndex(Recipe recipe) => recipe.RecipeIndex;

		private static Recipe RecipeIndexToRecipe(int index) => Main.recipe[index];

		private static bool TryGetEnabledRecipeByIndex(int index, out Recipe recipe) {
			recipe = null;
			if ((uint)index >= (uint)Main.recipe.Length)
				return false;

			recipe = RecipeIndexToRecipe(index);
			return recipe is not null && !recipe.Disabled;
		}

		/// <summary>
		/// Adds <paramref name="recipes"/> to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="recipes">An array of recipes to update.  If <see langword="null"/>, then nothing happens</param>
		public static void SetNextDefaultRecipeCollectionToRefresh(Recipe[] recipes) => AddOrUpdateRefreshingRecipesCollection(recipes);

		private static void AddOrUpdateRefreshingRecipesCollection(IEnumerable<Recipe> recipes) {
			if (recipes is null)
				return;

			recipesToRefreshByIndex ??= new();

			foreach (Recipe recipe in EnumerateRecipesWithRecursiveParents(recipes, GetConfiguredRecursionRefreshDepth())) {
				if (recipe is not null && !recipe.Disabled)
					recipesToRefreshByIndex.Add(RecipeToRecipeIndex(recipe));
			}
		}

		private static IEnumerable<Recipe> EnumerateRecipesWithRecursiveParents(IEnumerable<Recipe> toRefresh, int recursionDepth) {
			HashSet<int> seen = new();
			Queue<(int recipeIndex, int depth)> queue = new();

			foreach (Recipe recipe in toRefresh) {
				if (recipe is null || recipe.Disabled)
					continue;

				int recipeIndex = RecipeToRecipeIndex(recipe);
				if (seen.Add(recipeIndex)) {
					queue.Enqueue((recipeIndex, 0));
					yield return recipe;
				}
			}

			if (!MagicStorageConfig.IsRecursionEnabled)
				yield break;

			if (recursionDepth <= 0 || MagicCache.DirectRecursiveRecipeParentsByRecipeIndex is null)
				yield break;

			while (queue.Count > 0) {
				var (recipeIndex, depth) = queue.Dequeue();
				if (depth >= recursionDepth)
					continue;

				if (!MagicCache.DirectRecursiveRecipeParentsByRecipeIndex.TryGetValue(recipeIndex, out var parentRecipeIndexes))
					continue;

				foreach (int parentRecipeIndex in parentRecipeIndexes) {
					if (!seen.Add(parentRecipeIndex))
						continue;

					if (!TryGetEnabledRecipeByIndex(parentRecipeIndex, out Recipe parentRecipe))
						continue;

					queue.Enqueue((parentRecipeIndex, depth + 1));
					yield return parentRecipe;
				}
			}
		}

		private static int GetConfiguredRecursionRefreshDepth()
			=> MagicStorageConfig.IsRecursionInfinite ? UnboundedRecursionRefreshDepth : MagicStorageConfig.RecipeRecursionDepth;

		private static IEnumerable<Recipe> GetPossibleRecursionDependents(Recipe toRefresh) {
			if (!MagicStorageConfig.IsRecursionEnabled)
				return [];

			// The initial refresh set already expands the configured recursive closure.
			// Availability-change propagation only needs the next parent step as a safety net.
			return EnumerateRecursiveParentRecipes(toRefresh.RecipeIndex, recursionDepth: 1);
		}

		private static IEnumerable<Recipe> EnumerateRecursiveParentRecipes(int recipeIndex, int recursionDepth) {
			if (recursionDepth <= 0 || MagicCache.DirectRecursiveRecipeParentsByRecipeIndex is null)
				yield break;

			HashSet<int> seen = new() { recipeIndex };
			Queue<(int recipeIndex, int depth)> queue = new();
			queue.Enqueue((recipeIndex, 0));

			while (queue.Count > 0) {
				var (currentRecipeIndex, depth) = queue.Dequeue();
				if (depth >= recursionDepth)
					continue;

				if (!MagicCache.DirectRecursiveRecipeParentsByRecipeIndex.TryGetValue(currentRecipeIndex, out var parentRecipeIndexes))
					continue;

				foreach (int parentRecipeIndex in parentRecipeIndexes) {
					if (!seen.Add(parentRecipeIndex))
						continue;

					if (!TryGetEnabledRecipeByIndex(parentRecipeIndex, out Recipe parentRecipe))
						continue;

					queue.Enqueue((parentRecipeIndex, depth + 1));
					yield return parentRecipe;
				}
			}
		}

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedItemType"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemType">The item type to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(int affectedItemType) {
			if (affectedItemType <= ItemID.None || affectedItemType >= ItemLoader.ItemCount) {
				forceNextRecipeRefreshToBeFull = true;
				refreshInventorySnapshotForNextRecipeRefresh = true;
				return;
			}

			refreshInventorySnapshotForNextRecipeRefresh = true;
			itemTypesToRefresh ??= [];
			itemTypesToRefresh.Add(affectedItemType);

			if (MagicCache.RecipesUsingItemType.TryGetValue(affectedItemType, out var lazyResult))
				AddOrUpdateRefreshingRecipesCollection(lazyResult);
			else
				forceNextRecipeRefreshToBeFull = true;
		}

		/// <summary>
		/// Adds all recipes which use the IDs in <paramref name="affectedItemTypes"/> as an ingredient or result to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedItemTypes">A collection of item types to use when checking <see cref="MagicCache.RecipesUsingItemType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefresh(IEnumerable<int> affectedItemTypes) {
			if (affectedItemTypes is null)
				return;

			int[] itemTypes = [.. affectedItemTypes];
			if (itemTypes.Length <= 0)
				return;

			if (itemTypes.Any(static type => type <= ItemID.None || type >= ItemLoader.ItemCount)) {
				forceNextRecipeRefreshToBeFull = true;
				refreshInventorySnapshotForNextRecipeRefresh = true;
				return;
			}

			refreshInventorySnapshotForNextRecipeRefresh = true;
			itemTypesToRefresh ??= [];
			foreach (int itemType in itemTypes)
				itemTypesToRefresh.Add(itemType);

			AddOrUpdateRefreshingRecipesCollection(
				itemTypes.SelectManyByDictionary<int, Recipe[], Recipe>(MagicCache.RecipesUsingItemType)
			);
		}

		/// <summary>
		/// Adds all recipes which use <paramref name="affectedTileType"/> as a required tile to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileType">The tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(int affectedTileType) {
			if (affectedTileType < TileID.Dirt || affectedTileType >= TileLoader.TileCount) {
				forceNextRecipeRefreshToBeFull = true;
				return;
			}

			if (MagicCache.RecipesUsingTileType.TryGetValue(affectedTileType, out var lazyResult))
				AddOrUpdateRefreshingRecipesCollection(lazyResult);
			else
				forceNextRecipeRefreshToBeFull = true;
		}

		/// <summary>
		/// Adds all recipes which the IDs in <paramref name="affectedTileTypes"/> as a required tile to the collection of recipes to refresh when calling <see cref="MagicUI.RefreshItems"/>
		/// </summary>
		/// <param name="affectedTileTypes">A collection of the tile type to use when checking <see cref="MagicCache.RecipesUsingTileType"/></param>
		public static void SetNextDefaultRecipeCollectionToRefreshFromTile(IEnumerable<int> affectedTileTypes) {
			if (affectedTileTypes is null)
				return;

			int[] tileTypes = [.. affectedTileTypes];
			if (tileTypes.Any(static type => type < TileID.Dirt || type >= TileLoader.TileCount)) {
				forceNextRecipeRefreshToBeFull = true;
				return;
			}

			AddOrUpdateRefreshingRecipesCollection(
				tileTypes.SelectManyByDictionary<int, MagicCache.LazyRecipeTile, Recipe>(MagicCache.RecipesUsingTileType)
			);
		}
	}
}
