using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class AvailableRecipeObjects {
		private readonly bool[] tiles;
		private readonly Dictionary<int, int> inventory;
		private readonly bool[] recipeToConditionsAvailableCache;

		public AvailableRecipeObjects(bool[] tiles, Dictionary<int, int> inventory, bool[] recipeToConditionsAvailableCache = null) {
			this.tiles = tiles;
			this.inventory = inventory;
			this.recipeToConditionsAvailableCache = recipeToConditionsAvailableCache;
		}

		public bool IsTileAvailable(int tile) => tile >= 0 && tile < TileLoader.TileCount && tiles[tile];

		public bool IsRecipeAvailable(Recipe recipe) {
			if (recipeToConditionsAvailableCache is not null)
				return recipeToConditionsAvailableCache[recipe.RecipeIndex];

			return RecipeLoader.RecipeAvailable(recipe);
		}

		public bool CanUseRecipe(Recipe recipe) {
			foreach (int tile in recipe.requiredTile) {
				if (!IsTileAvailable(tile))
					return false;
			}

			return IsRecipeAvailable(recipe);
		}

		public int GetIngredientQuantity(int item) => inventory.TryGetValue(item, out int quantity) ? quantity : 0;

		public bool TryGetIngredientQuantity(int item, out int quantity) => inventory.TryGetValue(item, out quantity);

		public int GetTotalIngredientQuantity(Recipe recipe, int item) {
			int stack = 0;
			int quantity;

			bool usedRecipeGroup = false;
			foreach (int groupID in recipe.acceptedGroups) {
				RecipeGroup group = RecipeGroup.recipeGroups[groupID];
				if (group.ContainsItem(item)) {
					foreach (int groupItem in group.ValidItems) {
						if (TryGetIngredientQuantity(groupItem, out quantity)) {
							stack += quantity;
							usedRecipeGroup = true;
						}
					}
				}
			}

			if (!usedRecipeGroup && TryGetIngredientQuantity(item, out quantity))
				stack += quantity;

			return stack;
		}

		public IEnumerable<(int, int)> EnumerateInventory() {
			foreach (var (type, quantity) in inventory)
				yield return (type, quantity);
		}

		public int UpdateIngredient(int item, int amount) {
			if (amount > 0) {
				inventory.AddOrSumCount(item, amount);
				return 0;
			}

			amount = -amount;

			if (!TryGetIngredientQuantity(item, out int existing))
				return amount;

			if (existing > amount) {
				inventory[item] = existing - amount;
				amount = 0;
			} else {
				inventory.Remove(item);
				amount -= existing;
			}

			return amount;
		}

		public bool RemoveIngredient(int item) => inventory.Remove(item);
	}
}
