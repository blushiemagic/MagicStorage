using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Runtime.CompilerServices;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IRecipeSimulationsProvider {
		RecipeSimulations RecipeSimulations { get; }
	}

	public class RecipeSimulations {
		/// <summary>
		/// Stores amount-one recursive simulations produced by selected-preview and
		/// direct availability paths. Broad recipe-list refreshes must not publish
		/// preview simulations here.
		/// </summary>
		public readonly WeakTableProvider<Recipe, CraftingSimulation> recipeToAvailableSimulation;
		/// <summary>
		/// Stores the exact simulation for the selected recipe and requested craft amount.
		/// </summary>
		public readonly IValueProvider<CraftingSimulation> currentRecipeSimulation;
		/// <summary>
		/// Stores the shared inventory-centric craftability graph for the current refresh snapshot.
		/// </summary>
		public readonly IValueProvider<InventoryCraftabilityGraph> inventoryCraftabilityGraph;
		/// <summary>
		/// Stores the snapshot context used to build <see cref="inventoryCraftabilityGraph" />.
		/// </summary>
		public readonly IValueProvider<CraftingSimulationContext> inventoryCraftabilityGraphContext;

		public RecipeSimulations(ConditionalWeakTable<Recipe, CraftingSimulation> staticTable) {
			recipeToAvailableSimulation = new WeakTableProvider<Recipe, CraftingSimulation>(staticTable);
			currentRecipeSimulation = new ReadWriteValueProvider<CraftingSimulation>();
			inventoryCraftabilityGraph = new ReadWriteValueProvider<InventoryCraftabilityGraph>();
			inventoryCraftabilityGraphContext = new ReadWriteValueProvider<CraftingSimulationContext>();
		}

		public RecipeSimulations(
			ConditionalWeakTable<Recipe, CraftingSimulation> staticTable,
			CraftingSimulation cachedSimulation
		) {
			recipeToAvailableSimulation = new WeakTableProvider<Recipe, CraftingSimulation>(staticTable);
			currentRecipeSimulation = new ReadWriteValueProvider<CraftingSimulation>(cachedSimulation);
			inventoryCraftabilityGraph = new ReadWriteValueProvider<InventoryCraftabilityGraph>();
			inventoryCraftabilityGraphContext = new ReadWriteValueProvider<CraftingSimulationContext>();
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
