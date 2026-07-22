using System.Linq;
using System.Threading;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IRecipeSnapshotsProvider {
		RecipeSnapshots RecipeSnapshots { get; }
	}

	public class RecipeSnapshots {
		private static readonly object CachedConditionsLock = new();
		private static bool[] cachedFullConditions;
		private static int cachedFullConditionsHash;
		private static int cachedFullEnvironmentHash;
		private static int cachedFullRecipeCount;

		public bool[] ConditionsMet { get; private set; }
		public int ConditionsHash { get; private set; }

		public static void ClearCachedConditions() {
			lock (CachedConditionsLock) {
				cachedFullConditions = null;
				cachedFullConditionsHash = 0;
				cachedFullEnvironmentHash = 0;
				cachedFullRecipeCount = 0;
			}
		}

		public void CollectObjects(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			int environmentHash = CraftingGUI.GetCraftingEnvironmentHash();

			lock (CachedConditionsLock) {
				if (cachedFullConditions is not null
				&& cachedFullEnvironmentHash == environmentHash
				&& cachedFullRecipeCount == Recipe.numRecipes) {
					ConditionsMet = cachedFullConditions;
					ConditionsHash = cachedFullConditionsHash;
					return;
				}
			}

			bool[] conditions = CraftingGUI.ExecuteInCraftingGuiEnvironment(() => CollectConditionSnapshot(cancellationToken));
			cancellationToken.ThrowIfCancellationRequested();
			int conditionsHash = GetBoolArrayHash(conditions);

			lock (CachedConditionsLock) {
				cachedFullConditions = conditions;
				cachedFullConditionsHash = conditionsHash;
				cachedFullEnvironmentHash = environmentHash;
				cachedFullRecipeCount = Recipe.numRecipes;
			}

			ConditionsMet = conditions;
			ConditionsHash = conditionsHash;
		}

		private static bool[] CollectConditionSnapshot(CancellationToken cancellationToken) {
			bool[] conditions = new bool[Recipe.numRecipes];

			for (int i = 0; i < Recipe.numRecipes; i++) {
				cancellationToken.ThrowIfCancellationRequested();
				conditions[i] = Utility.IsAvailableForSnapshot(Main.recipe[i]);
			}

			return conditions;
		}

		public void CollectSingleObject(Recipe recipe) {
			ConditionsMet = new bool[Recipe.numRecipes];

			if (recipe is null) {
				ConditionsHash = GetBoolArrayHash(ConditionsMet);
				return;
			}

			ConditionsMet[recipe.RecipeIndex] = CraftingGUI.ExecuteInCraftingGuiEnvironment(recipe, Utility.IsAvailableForSnapshot);
			ConditionsHash = GetBoolArrayHash(ConditionsMet);
		}

		private static int GetBoolArrayHash(bool[] values) {
			if (values is null)
				return 0;

			var hash = new System.HashCode();
			hash.Add(values.Length);

			for (int i = 0; i < values.Length; i++)
				hash.Add(values[i]);

			return hash.ToHashCode();
		}
	}
}
