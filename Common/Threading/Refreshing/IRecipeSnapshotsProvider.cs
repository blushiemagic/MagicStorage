using System.Linq;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IRecipeSnapshotsProvider {
		RecipeSnapshots RecipeSnapshots { get; }
	}

	public class RecipeSnapshots {
		public bool[] ConditionsMet { get; private set; }

		public void CollectObjects() {
			ConditionsMet = CraftingGUI.ExecuteInCraftingGuiEnvironment<bool[]>(() => [.. Main.recipe.Take(Recipe.numRecipes).Select(Utility.IsAvailableForSnapshot)]);
		}
	}
}
