using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public readonly struct CraftResult {
		public readonly List<RecursedRecipe> usedRecipes;
		public readonly List<RequiredMaterialInfo> requiredMaterials;
		public readonly List<ExcessItemInfo> excessResults;
		public readonly HashSet<int> requiredTiles;
		public readonly HashSet<Condition> requiredConditions;

		public bool WasAvailable { get; }

		public static CraftResult Default => new CraftResult(new(), new(), new(), new(), new(ReferenceEqualityComparer.Instance));

		public CraftResult(List<RecursedRecipe> recipes, List<RequiredMaterialInfo> materials, List<ExcessItemInfo> excess, HashSet<int> tiles, HashSet<Condition> conditions) {
			usedRecipes = recipes;
			requiredMaterials = materials;
			excessResults = excess;
			requiredTiles = tiles;
			requiredConditions = conditions;

			WasAvailable = true;
		}

		public CraftResult CombineWith(in CraftResult other) {
			if (!WasAvailable && other.WasAvailable)
				return other;
			else if (WasAvailable && !other.WasAvailable)
				return this;
			else if (!WasAvailable && !other.WasAvailable)
				return default;

			var recipes = new List<RecursedRecipe>();
			var materials = new List<RequiredMaterialInfo>(requiredMaterials);
			var excess = new List<ExcessItemInfo>(excessResults);
			var tiles = new HashSet<int>(requiredTiles);
			var conditions = new HashSet<Condition>(requiredConditions, ReferenceEqualityComparer.Instance);

			// Copy to make manipulation not affect the caller's scope
			var otherMaterials = new List<RequiredMaterialInfo>(other.requiredMaterials);

			// For each new material required, remove them from the excess results first
			if (excess.Count > 0) {
				for (int i = 0; i < otherMaterials.Count; i++) {
					var mat = otherMaterials[i];

					if (mat.Stack <= 0)
						continue;  // Empty material, ignore

					for (int j = 0; j < excess.Count; j++) {
						var info = excess[j];

						if (info.Stack <= 0)
							continue;  // Fully consumed, empty slot

						foreach (int item in mat.GetValidItems()) {
							if (info.type == item) {
								// Item type matched, attempt to consume from excess materials list
								if (info.Stack >= mat.Stack) {
									info.UpdateStack(-mat.Stack);
									mat.ClearStack();
									goto checkNextMaterial;
								} else {
									mat.UpdateStack(-info.Stack);
									info.ClearStack();
								}
							}
						}
					}

					checkNextMaterial: ;
				}
			}

			// Merge the excess results
			foreach (ExcessItemInfo info in other.excessResults) {
				if (info.Stack <= 0)
					continue;  // Fully consumed, empty slot

				int index = excess.FindIndex(i => i.EqualsIgnoreStack(info));
				if (index < 0)
					excess.Add(info);
				else
					excess[index].UpdateStack(info.Stack);
			}

			// Merge everything else
			materials.AddRange(otherMaterials);
			recipes.AddRange(usedRecipes.Concat(other.usedRecipes).DistinctBy(static r => r, RecursedRecipeComparer.Instance));
			tiles.UnionWith(other.requiredTiles);
			conditions.UnionWith(other.requiredConditions);

			return new CraftResult(recipes, materials, excess, tiles, conditions);
		}
	}
}
