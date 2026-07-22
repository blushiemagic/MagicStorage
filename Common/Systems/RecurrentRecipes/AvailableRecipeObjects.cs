using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class AvailableRecipeObjects {
		private readonly bool[] tiles;
		private readonly Dictionary<int, int> inventory;
		private readonly Dictionary<int, int> inventoryOverlay;
		private readonly bool[] recipeToConditionsAvailableCache;
		public readonly HashSet<int> isItemInfinite;
		public readonly bool creativeUnitPresent;

		internal AvailableRecipeObjects(bool[] tiles, Dictionary<int, int> inventory, bool[] recipeToConditionsAvailableCache, HashSet<int> isItemInfinite, bool creativeUnitPresent) {
			this.tiles = tiles;
			this.inventory = inventory;
			this.recipeToConditionsAvailableCache = recipeToConditionsAvailableCache;
			this.isItemInfinite = isItemInfinite;
			this.creativeUnitPresent = creativeUnitPresent;
		}

		private AvailableRecipeObjects(bool[] tiles, Dictionary<int, int> inventory, Dictionary<int, int> inventoryOverlay, bool[] recipeToConditionsAvailableCache, HashSet<int> isItemInfinite, bool creativeUnitPresent) {
			this.tiles = tiles;
			this.inventory = inventory;
			this.inventoryOverlay = inventoryOverlay;
			this.recipeToConditionsAvailableCache = recipeToConditionsAvailableCache;
			this.isItemInfinite = isItemInfinite;
			this.creativeUnitPresent = creativeUnitPresent;
		}

		/// <summary>
		/// Creates a mutable overlay snapshot for recursive crafting plan probes.
		/// </summary>
		public AvailableRecipeObjects CloneForSimulation()
			=> new AvailableRecipeObjects(tiles, inventory, new Dictionary<int, int>(), recipeToConditionsAvailableCache, isItemInfinite, creativeUnitPresent);

		public bool IsItemInfinite(int item) => creativeUnitPresent || isItemInfinite.Contains(item);

		public bool IsTileAvailable(int tile) => tile >= 0 && tile < TileLoader.TileCount && tiles[tile];

		public bool IsRecipeAvailable(Recipe recipe) {
			if (recipeToConditionsAvailableCache is not null)
				return recipeToConditionsAvailableCache[recipe.RecipeIndex];

			// Cache is not present; use the Crafting Interface's context to check if the recipe is available
			return CraftingGUI.ExecuteInCraftingGuiEnvironment(recipe, RecipeLoader.RecipeAvailable);
		}

		public bool CanUseRecipe(Recipe recipe) {
			foreach (int tile in recipe.requiredTile) {
				if (!IsTileAvailable(tile))
					return false;
			}

			return IsRecipeAvailable(recipe);
		}

		public int GetIngredientQuantity(int item) {
			if (IsItemInfinite(item))
				return int.MaxValue;

			if (inventoryOverlay is not null && inventoryOverlay.TryGetValue(item, out int overlayQuantity))
				return overlayQuantity;

			return inventory.TryGetValue(item, out int quantity) ? quantity : 0;
		}

		public bool TryGetIngredientQuantity(int item, out int quantity) {
			if (IsItemInfinite(item)) {
				quantity = int.MaxValue;
				return true;
			}

			if (inventoryOverlay is not null && inventoryOverlay.TryGetValue(item, out quantity))
				return quantity > 0;

			return inventory.TryGetValue(item, out quantity);
		}

		public int GetTotalIngredientQuantity(Recipe recipe, int item) {
			if (IsItemInfinite(item))
				return int.MaxValue;

			ClampedArithmetic stack = 0;
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
			if (inventoryOverlay is not null) {
				foreach (var (type, quantity) in inventory) {
					if (inventoryOverlay.ContainsKey(type))
						continue;

					int _quantity = quantity;
					if (IsItemInfinite(type))
						_quantity = int.MaxValue;

					yield return (type, _quantity);
				}

				foreach (var (type, quantity) in inventoryOverlay) {
					if (quantity <= 0)
						continue;

					int _quantity = quantity;
					if (IsItemInfinite(type))
						_quantity = int.MaxValue;

					yield return (type, _quantity);
				}

				yield break;
			}

			foreach (var (type, quantity) in inventory) {
				int _quantity = quantity;
				if (IsItemInfinite(type))
					_quantity = int.MaxValue;

				yield return (type, _quantity);
			}
		}

		internal bool TryGetInventoryFingerprintEntry(int item, out int quantity) {
			if (inventoryOverlay is not null && inventoryOverlay.TryGetValue(item, out int overlayQuantity)) {
				if (overlayQuantity <= 0) {
					quantity = 0;
					return false;
				}

				quantity = IsItemInfinite(item) ? int.MaxValue : overlayQuantity;
				return true;
			}

			if (!inventory.TryGetValue(item, out quantity) || quantity <= 0) {
				quantity = 0;
				return false;
			}

			if (IsItemInfinite(item))
				quantity = int.MaxValue;

			return true;
		}

		public int UpdateIngredient(int item, int amount) {
			if (IsItemInfinite(item))
				return 0;

			if (inventoryOverlay is not null)
				return UpdateIngredientOverlay(item, amount);

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

		private int UpdateIngredientOverlay(int item, int amount) {
			if (amount > 0) {
				int existingQuantity = GetIngredientQuantity(item);
				inventoryOverlay[item] = existingQuantity + amount;
				return 0;
			}

			amount = -amount;

			if (!TryGetIngredientQuantity(item, out int existing))
				return amount;

			if (existing > amount) {
				inventoryOverlay[item] = existing - amount;
				return 0;
			}

			inventoryOverlay[item] = 0;
			return amount - existing;
		}

		public bool RemoveIngredient(int item) {
			if (inventoryOverlay is not null) {
				bool existed = GetIngredientQuantity(item) > 0;
				inventoryOverlay[item] = 0;
				return existed;
			}

			return inventory.Remove(item);
		}
	}
}
