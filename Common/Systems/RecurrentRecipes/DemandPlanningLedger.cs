using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	internal sealed class DemandPlanningLedger {
		private readonly AvailableRecipeObjects inventory;
		private readonly List<(int Type, int Quantity)> inventoryChanges = new();
		private Dictionary<int, int> requiredMaterials = new();
		private Dictionary<ExcessKey, int> excessResults = new();
		private Dictionary<int, int> producedByType = new();
		private List<RecursedRecipe> usedRecipes = new();
		private HashSet<int> requiredTiles = new();
		private HashSet<Condition> requiredConditions = new(ReferenceEqualityComparer.Instance);
		private int inventoryFingerprint;
		private bool rollingBack;

		public DemandPlanningLedger(AvailableRecipeObjects available) {
			inventory = available.CloneForSimulation();
			inventoryFingerprint = ComputeInventoryFingerprint();
		}

		public int GetIngredientQuantity(int itemType) => inventory.GetIngredientQuantity(itemType);

		public bool IsItemInfinite(int itemType) => inventory.IsItemInfinite(itemType);

		public bool IsTileAvailable(int tileType) => inventory.IsTileAvailable(tileType);

		public bool CanUseRecipe(Recipe recipe) => inventory.CanUseRecipe(recipe);

		public int GetInventoryFingerprint() => inventoryFingerprint;

		public DemandPlanningCheckpoint CreateCheckpoint()
			=> new(
				inventoryChanges.Count,
				new Dictionary<int, int>(requiredMaterials),
				new Dictionary<ExcessKey, int>(excessResults),
				new Dictionary<int, int>(producedByType),
				usedRecipes.Count,
				new HashSet<int>(requiredTiles),
				new HashSet<Condition>(requiredConditions, ReferenceEqualityComparer.Instance),
				inventoryFingerprint
			);

		public void Commit(DemandPlanningCheckpoint checkpoint) {
			// Keep the change log intact: an outer checkpoint may still need to
			// roll back this committed branch if its parent candidate fails.
		}

		public void Rollback(DemandPlanningCheckpoint checkpoint) {
			rollingBack = true;
			try {
				for (int i = inventoryChanges.Count - 1; i >= checkpoint.InventoryChangeCount; i--) {
					var (type, quantity) = inventoryChanges[i];
					RestoreIngredientQuantity(type, quantity);
				}

				inventoryChanges.RemoveRange(checkpoint.InventoryChangeCount, inventoryChanges.Count - checkpoint.InventoryChangeCount);
			} finally {
				rollingBack = false;
			}

			requiredMaterials = new Dictionary<int, int>(checkpoint.RequiredMaterials);
			excessResults = new Dictionary<ExcessKey, int>(checkpoint.ExcessResults);
			producedByType = new Dictionary<int, int>(checkpoint.ProducedByType);

			if (usedRecipes.Count > checkpoint.UsedRecipeCount)
				usedRecipes.RemoveRange(checkpoint.UsedRecipeCount, usedRecipes.Count - checkpoint.UsedRecipeCount);

			requiredTiles = new HashSet<int>(checkpoint.RequiredTiles);
			requiredConditions = new HashSet<Condition>(checkpoint.RequiredConditions, ReferenceEqualityComparer.Instance);
			inventoryFingerprint = checkpoint.InventoryFingerprint;
		}

		public bool TryConsumeItem(int itemType, int quantity) {
			if (quantity <= 0 || IsItemInfinite(itemType))
				return true;

			if (producedByType.TryGetValue(itemType, out int producedQuantity) && producedQuantity > 0) {
				int consumedFromProduced = Math.Min(producedQuantity, quantity);
				UpdateIngredient(itemType, -consumedFromProduced);
				DecreaseProduced(itemType, consumedFromProduced);
				DecreaseExcess(itemType, consumedFromProduced);
				quantity -= consumedFromProduced;

				if (quantity <= 0)
					return true;
			}

			if (GetIngredientQuantity(itemType) < quantity)
				return false;

			UpdateIngredient(itemType, -quantity);
			requiredMaterials.AddOrSumCount(itemType, quantity);
			return true;
		}

		public int ConsumeAvailableItem(int itemType, int maxQuantity) {
			if (maxQuantity <= 0 || IsItemInfinite(itemType))
				return maxQuantity;

			int availableQuantity = Math.Min(GetIngredientQuantity(itemType), maxQuantity);
			if (availableQuantity <= 0)
				return 0;

			return TryConsumeItem(itemType, availableQuantity) ? availableQuantity : 0;
		}

		public int ConsumeAvailableRecipeGroup(RecipeGroup group, int maxQuantity) {
			if (group is null || maxQuantity <= 0)
				return 0;

			int consumed = 0;
			foreach (int itemType in group.ValidItems
				.Select(type => (Type: type, Quantity: GetIngredientQuantity(type)))
				.Where(static entry => entry.Quantity > 0)
				.OrderByDescending(static entry => entry.Quantity)
				.ThenBy(static entry => entry.Type)
				.Select(static entry => entry.Type)) {
				int remaining = maxQuantity - consumed;
				if (remaining <= 0)
					break;

				consumed += ConsumeAvailableItem(itemType, remaining);
			}

			return consumed;
		}

		public void RecordRecipeCraft(Recipe recipe, int batches, int depth) {
			usedRecipes.Add(new RecursedRecipe(depth, recipe));
			requiredTiles.UnionWith(recipe.requiredTile);
			requiredConditions.UnionWith(recipe.Conditions);

			EnvironmentSandbox sandbox;
			IEnumerable<EnvironmentModule> modules;
			if (CraftingGUI.GetHeart() is { } heart) {
				sandbox = new EnvironmentSandbox(Main.LocalPlayer, heart);
				modules = heart.GetModules();
			} else {
				sandbox = default;
				modules = [];
			}

			CraftingGUI.DroppedItems ??= new();

			for (int i = 0; i < batches; i++) {
				foreach (Item item in ExtraCraftItemsSystem.GetSimulatedItemDrops(recipe))
					ProduceItem(item.type, item.stack, item.prefix);

				foreach (EnvironmentModule module in modules)
					module.OnConsumeItemsForRecipe(sandbox, recipe, recipe.requiredItem);
			}

			Item createItem = recipe.createItem.Clone();
			createItem.Prefix(-1);
			ProduceItem(createItem.type, createItem.stack * batches, createItem.prefix);
		}

		public CraftResult ToCraftResult() {
			var materials = requiredMaterials
				.Select(static entry => RequiredMaterialInfo.FromItem(entry.Key, new SharedCounter(entry.Value)))
				.ToList();

			var excess = excessResults
				.Select(static entry => new ExcessItemInfo(entry.Key.Type, new SharedCounter(entry.Value), entry.Key.Prefix))
				.ToList();

			return new CraftResult(
				new List<RecursedRecipe>(usedRecipes.DistinctBy(static r => r, RecursedRecipeComparer.Instance)),
				materials,
				excess,
				new HashSet<int>(requiredTiles),
				new HashSet<Condition>(requiredConditions, ReferenceEqualityComparer.Instance)
			);
		}

		private void ProduceItem(int itemType, int quantity, int prefix) {
			if (quantity <= 0)
				return;

			UpdateIngredient(itemType, quantity);
			producedByType.AddOrSumCount(itemType, quantity);
			AddOrSumExcess(new ExcessKey(itemType, prefix), quantity);
		}

		private void AddOrSumExcess(ExcessKey key, int quantity) {
			if (excessResults.TryGetValue(key, out int existing))
				excessResults[key] = existing + quantity;
			else
				excessResults[key] = quantity;
		}

		private void UpdateIngredient(int itemType, int quantity) {
			bool hadOldEntry = inventory.TryGetInventoryFingerprintEntry(itemType, out int oldQuantity);

			if (!rollingBack)
				inventoryChanges.Add((itemType, GetIngredientQuantity(itemType)));

			inventory.UpdateIngredient(itemType, quantity);

			if (rollingBack)
				return;

			bool hasNewEntry = inventory.TryGetInventoryFingerprintEntry(itemType, out int newQuantity);
			if (hadOldEntry)
				inventoryFingerprint ^= GetInventoryEntryHash(itemType, oldQuantity);

			if (hasNewEntry)
				inventoryFingerprint ^= GetInventoryEntryHash(itemType, newQuantity);
		}

		private void RestoreIngredientQuantity(int itemType, int quantity) {
			if (IsItemInfinite(itemType))
				return;

			int currentQuantity = GetIngredientQuantity(itemType);
			if (currentQuantity == quantity)
				return;

			inventory.UpdateIngredient(itemType, quantity - currentQuantity);
		}

		private void DecreaseProduced(int itemType, int quantity) {
			int remaining = producedByType[itemType] - quantity;
			if (remaining > 0)
				producedByType[itemType] = remaining;
			else
				producedByType.Remove(itemType);
		}

		private void DecreaseExcess(int itemType, int quantity) {
			foreach (ExcessKey key in excessResults.Keys.Where(key => key.Type == itemType).ToArray()) {
				int stack = excessResults[key];
				if (stack > quantity) {
					excessResults[key] = stack - quantity;
					return;
				}

				excessResults.Remove(key);
				quantity -= stack;

				if (quantity <= 0)
					return;
			}
		}

		private int ComputeInventoryFingerprint() {
			int hash = 0;

			foreach (var (type, quantity) in inventory.EnumerateInventory())
				hash ^= GetInventoryEntryHash(type, quantity);

			return hash;
		}

		private static int GetInventoryEntryHash(int type, int quantity)
			=> HashCode.Combine(type, quantity);

		internal readonly record struct ExcessKey(int Type, int Prefix);
	}

	internal readonly struct DemandPlanningCheckpoint {
		public int InventoryChangeCount { get; }
		public Dictionary<int, int> RequiredMaterials { get; }
		public Dictionary<DemandPlanningLedger.ExcessKey, int> ExcessResults { get; }
		public Dictionary<int, int> ProducedByType { get; }
		public int UsedRecipeCount { get; }
		public HashSet<int> RequiredTiles { get; }
		public HashSet<Condition> RequiredConditions { get; }
		public int InventoryFingerprint { get; }

		public DemandPlanningCheckpoint(
			int inventoryChangeCount,
			Dictionary<int, int> requiredMaterials,
			Dictionary<DemandPlanningLedger.ExcessKey, int> excessResults,
			Dictionary<int, int> producedByType,
			int usedRecipeCount,
			HashSet<int> requiredTiles,
			HashSet<Condition> requiredConditions,
			int inventoryFingerprint
		) {
			InventoryChangeCount = inventoryChangeCount;
			RequiredMaterials = requiredMaterials;
			ExcessResults = excessResults;
			ProducedByType = producedByType;
			UsedRecipeCount = usedRecipeCount;
			RequiredTiles = requiredTiles;
			RequiredConditions = requiredConditions;
			InventoryFingerprint = inventoryFingerprint;
		}
	}
}
