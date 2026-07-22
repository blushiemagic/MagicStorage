using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class RecipeIngredientInfo {
		public readonly RecipeInfo parent;

		public readonly int recipeIngredientIndex;

		internal readonly Recipe[] recipes;

		private readonly object nodesLock = new();

		private Node[] nodes;

		/// <summary>
		/// How many recipes can create this ingredient
		/// </summary>
		public int RecipeCount => recipes.Length;

		internal RecipeIngredientInfo(RecipeInfo recipeInfo, int index) {
			parent = recipeInfo;
			recipeIngredientIndex = index;

			// Account for recipe groups as well
			int recipeItem = recipeInfo.sourceRecipe.requiredItem[index].type;
			HashSet<int> types = new() { recipeItem };

			foreach (int id in recipeInfo.sourceRecipe.acceptedGroups) {
				RecipeGroup group = RecipeGroup.recipeGroups[id];
				if (group.ContainsItem(recipeItem))
					types.UnionWith(group.ValidItems);
			}

			List<Recipe> matchingRecipes = new();
			HashSet<Recipe> seenRecipes = new(ReferenceEqualityComparer.Instance);

			foreach (int type in types) {
				if (!MagicCache.ResultToRecipe.TryGetValue(type, out Recipe[] recipesForType))
					continue;

				foreach (Recipe recipe in recipesForType) {
					if (!recipe.Disabled && !MagicCache.IsRecipeBlocked(recipe) && seenRecipes.Add(recipe))
						matchingRecipes.Add(recipe);
				}
			}

			recipes = matchingRecipes.ToArray();
		}

		internal void ClearTrees() {
			nodes = null;
		}

		internal Node[] GetOrCreateNodes() {
			if (nodes is not null)
				return nodes;

			lock (nodesLock) {
				if (nodes is not null)
					return nodes;

				List<Node> matchingNodes = new(recipes.Length);
				foreach (Recipe recipe in recipes) {
					Node node = NodePool.FindOrCreate(recipe);
					if (node is not null)
						matchingNodes.Add(node);
				}

				nodes = matchingNodes.ToArray();
				return nodes;
			}
		}

		/// <summary>
		/// Returns all possible recipes that can craft this ingredient, given the available inventory
		/// </summary>
		/// <param name="available">
		/// An optional object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met.<br/>
		/// If <see langword="null"/>, all recipes are returned.
		/// </param>
		/// <param name="blockedRecipeIngredient">
		/// An optional item ID representing a recipe ingredient that should not be used when finding the best match.<br/>
		/// If this parameter is greater than 0, then any recipes using the blocked item ID as a possible ingredient will be skipped.
		/// </param>
		public IEnumerable<Recipe> EnumerateValidRecipes(AvailableRecipeObjects available, int blockedRecipeIngredient = 0) {
			if (available is null) {
				// Assume that the caller handles null inventory and use all recipes
				foreach (Recipe recipe in recipes)
					yield return recipe;

				yield break;
			}

			if (recipes.Length < 2) {
				if (recipes.Length > 0 && available.CanUseRecipe(recipes[0]))
					yield return recipes[0];

				yield break;
			}

			bool anyDirectlyAvailable = false;
			List<Recipe> recursiveFallbacks = null;

			for (int i = 0; i < recipes.Length; i++) {
				Recipe subrecipe = recipes[i];

				// Not enough stations or conditions met?  Skip this recipe
				if (!available.CanUseRecipe(subrecipe))
					goto checkNextTree;

				if (RecipeUsesBlockedIngredient(subrecipe, blockedRecipeIngredient))
					goto checkNextTree;

				if (AreRecipeIngredientsDirectlyAvailable(subrecipe, available)) {
					anyDirectlyAvailable = true;
					yield return subrecipe;
				} else if (RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(subrecipe, out _)) {
					recursiveFallbacks ??= [];
					recursiveFallbacks.Add(subrecipe);
				}

				checkNextTree: ;
			}

			if (!anyDirectlyAvailable && recursiveFallbacks is not null) {
				foreach (Recipe recipe in recursiveFallbacks)
					yield return recipe;
			}
		}

		private static bool RecipeUsesBlockedIngredient(Recipe recipe, int blockedRecipeIngredient) {
			if (blockedRecipeIngredient <= 0)
				return false;

			foreach (Item item in recipe.requiredItem) {
				if (item.type == blockedRecipeIngredient)
					return true;

				foreach (int groupID in recipe.acceptedGroups) {
					RecipeGroup group = RecipeGroup.recipeGroups[groupID];
					if (!group.ContainsItem(item.type))
						continue;

					foreach (int groupItem in group.ValidItems) {
						if (groupItem == blockedRecipeIngredient)
							return true;
					}
				}
			}

			return false;
		}

		private static bool AreRecipeIngredientsDirectlyAvailable(Recipe recipe, AvailableRecipeObjects available) {
			foreach (Item item in recipe.requiredItem) {
				bool usedRecipeGroup = false;
				ClampedArithmetic stack = item.stack;

				int count;
				foreach (int groupID in recipe.acceptedGroups) {
					RecipeGroup group = RecipeGroup.recipeGroups[groupID];

					if (group.ContainsItem(item.type)) {
						foreach (int groupItem in group.ValidItems) {
							if (available.TryGetIngredientQuantity(groupItem, out count)) {
								usedRecipeGroup = true;
								stack -= count;

								if (stack <= 0)
									goto checkNonRecipeGroup;
							}
						}
					}
				}

				checkNonRecipeGroup:

				if (!usedRecipeGroup && available.TryGetIngredientQuantity(item.type, out count))
					stack -= count;

				if (stack > 0)
					return false;
			}

			return true;
		}
	}
}
