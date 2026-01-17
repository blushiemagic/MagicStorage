using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Runtime.CompilerServices;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IRecipeSimulationsProvider {
		RecipeSimulations RecipeSimulations { get; }
	}

	public class RecipeSimulations {
		public readonly WeakTableProvider<Recipe, CraftingSimulation> recipeToAvailableSimulation;
		public readonly IValueProvider<CraftingSimulation> currentRecipeSimulation;

		public RecipeSimulations(ConditionalWeakTable<Recipe, CraftingSimulation> staticTable) {
			recipeToAvailableSimulation = new WeakTableProvider<Recipe, CraftingSimulation>(staticTable);
			currentRecipeSimulation = new ReadWriteValueProvider<CraftingSimulation>();
		}

		public RecipeSimulations(
			ConditionalWeakTable<Recipe, CraftingSimulation> staticTable,
			CraftingSimulation cachedSimulation
		) {
			recipeToAvailableSimulation = new WeakTableProvider<Recipe, CraftingSimulation>(staticTable);
			currentRecipeSimulation = new ReadWriteValueProvider<CraftingSimulation>(cachedSimulation);
		}

		public void CopyFromStaticCollection() {
			recipeToAvailableSimulation.CopyFromStatic();
		}

		public void CopyToStaticCollection() {
			recipeToAvailableSimulation.CopyToStatic();
		}

		public void ClearStaticCollection() {
			recipeToAvailableSimulation.ClearStatic();
		}
	}
}
