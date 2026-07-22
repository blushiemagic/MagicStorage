using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class CraftingSimulation {
		private CraftResult simulationResult = CraftResult.Default;

		public IEnumerable<Recipe> UsedRecipes => simulationResult.usedRecipes.OrderBy(static r => r.recursionDepth).Select(static r => r.recipe).DistinctBy(static r => r, ReferenceEqualityComparer.Instance);
		public IReadOnlyList<RequiredMaterialInfo> RequiredMaterials => simulationResult.requiredMaterials;
		public IReadOnlyList<ExcessItemInfo> ExcessResults => simulationResult.excessResults;
		public IEnumerable<int> RequiredTiles => simulationResult.requiredTiles;
		public IEnumerable<Condition> RequiredConditions => simulationResult.requiredConditions;

		public Recipe Recipe { get; private set; }
		public int AmountCrafted { get; private set; }
		public int RequestedAmount { get; private set; }
		public CraftingSimulationContext Context { get; private set; }
		internal bool IsGraphBacked { get; private set; }

		public bool HasCondition(Condition condition) {
			return simulationResult.requiredConditions.Contains(condition);
		}

		public bool UsedRecipe(Recipe recipe) {
			return simulationResult.usedRecipes.Any(r => object.ReferenceEquals(r.recipe, recipe));
		}

		/// <summary>
		/// Initializes this simulation as a failed exact result for a known recipe and snapshot context.
		/// </summary>
		public void SetFailed(Recipe recipe, int amountToCraft, CraftingSimulationContext context = default) {
			simulationResult = CraftResult.Default;
			Recipe = recipe;
			RequestedAmount = amountToCraft;
			Context = context;
			AmountCrafted = 0;
			IsGraphBacked = false;
		}

		public void SimulateCrafts(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context = default, CancellationToken cancellationToken = default) {
			using (FlagSwitch.ToggleTrue(ref CraftingGUI._simulatingCrafts))
				SimulateCrafts_Inner(recipe, amountToCraft, available, context, cancellationToken);
		}

		internal bool TryTraceExactCrafts(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context, ExactCraftingTrace trace, CancellationToken cancellationToken = default) {
			using (FlagSwitch.ToggleTrue(ref CraftingGUI._simulatingCrafts))
				return TryTraceExactCrafts_Inner(recipe, amountToCraft, available, context, trace, cancellationToken);
		}

		public bool TryPlanCraftsWithGraph(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, InventoryCraftabilityGraph graph, CraftingSimulationContext context = default, CancellationToken cancellationToken = default) {
			using (FlagSwitch.ToggleTrue(ref CraftingGUI._simulatingCrafts))
				return TryPlanCraftsWithGraph_Inner(recipe, amountToCraft, available, graph, context, useDiagnosticAuthority: false, cancellationToken);
		}

		internal bool TryPlanCraftsWithGraphForDiagnostics(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, InventoryCraftabilityGraph graph, CraftingSimulationContext context = default, CancellationToken cancellationToken = default) {
			using (FlagSwitch.ToggleTrue(ref CraftingGUI._simulatingCrafts))
				return TryPlanCraftsWithGraph_Inner(recipe, amountToCraft, available, graph, context, useDiagnosticAuthority: true, trace: null, cancellationToken);
		}

		internal bool TryPlanCraftsWithGraphForDiagnostics(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, InventoryCraftabilityGraph graph, CraftingSimulationContext context, DemandPlanningTrace trace, CancellationToken cancellationToken = default) {
			using (FlagSwitch.ToggleTrue(ref CraftingGUI._simulatingCrafts))
				return TryPlanCraftsWithGraph_Inner(recipe, amountToCraft, available, graph, context, useDiagnosticAuthority: true, trace, cancellationToken);
		}

		private void SimulateCrafts_Inner(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			simulationResult = CraftResult.Default;
			Recipe = recipe.original;
			RequestedAmount = amountToCraft;
			Context = context;
			IsGraphBacked = false;

			int craftingTarget = Math.Min(amountToCraft, Item.CommonMaxStack);
			if (craftingTarget <= 0) {
				AmountCrafted = 0;
				return;
			}

			int mainResultItem = recipe.original.createItem.type;
			if (TryPlanCrafts(recipe, craftingTarget, available.CloneForSimulation(), mainResultItem, out CraftResult exactResult, cancellationToken)) {
				simulationResult = exactResult;
				AmountCrafted = craftingTarget;
				return;
			}

			int craftable = FindMaxCraftable(recipe, craftingTarget, available, mainResultItem, out CraftResult bestResult, cancellationToken);
			if (craftable > 0)
				simulationResult = bestResult;

			AmountCrafted = craftable;
		}

		private bool TryTraceExactCrafts_Inner(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, CraftingSimulationContext context, ExactCraftingTrace trace, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			simulationResult = CraftResult.Default;
			Recipe = recipe.original;
			RequestedAmount = amountToCraft;
			Context = context;
			AmountCrafted = 0;
			IsGraphBacked = false;

			int craftingTarget = Math.Min(amountToCraft, Item.CommonMaxStack);
			trace?.Add(0, $"exact root {DemandPlanningTrace.DescribeRecipe(recipe.original)} amount={craftingTarget}");
			if (craftingTarget <= 0)
				return false;

			int mainResultItem = recipe.original.createItem.type;
			recipe.GetCraftingInformation(craftingTarget, out CraftResult exactResult, available.CloneForSimulation(), mainResultItem, trace, cancellationToken);
			if (!exactResult.WasAvailable) {
				trace?.Add(0, "exact result unavailable before replay");
				return false;
			}

			if (!ApplyCraftResult(exactResult, available.CloneForSimulation(), cancellationToken, trace)) {
				trace?.Add(0, "exact replay failed");
				return false;
			}

			simulationResult = exactResult;
			AmountCrafted = craftingTarget;
			trace?.Add(0, "exact replay succeeded");
			return true;
		}

		private bool TryPlanCraftsWithGraph_Inner(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, InventoryCraftabilityGraph graph, CraftingSimulationContext context, bool useDiagnosticAuthority, CancellationToken cancellationToken) {
			return TryPlanCraftsWithGraph_Inner(recipe, amountToCraft, available, graph, context, useDiagnosticAuthority, trace: null, cancellationToken);
		}

		private bool TryPlanCraftsWithGraph_Inner(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, InventoryCraftabilityGraph graph, CraftingSimulationContext context, bool useDiagnosticAuthority, DemandPlanningTrace trace, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			simulationResult = CraftResult.Default;
			Recipe = recipe.original;
			RequestedAmount = amountToCraft;
			Context = context;
			AmountCrafted = 0;
			IsGraphBacked = false;

			int craftingTarget = Math.Min(amountToCraft, Item.CommonMaxStack);
			if (craftingTarget <= 0 || graph is not { CanRejectMissingRecipes: true })
				return false;

			InventoryCraftabilityRecipeProbe probe = graph.ProbeRecipe(recipe.original);
			trace?.Add(0, $"root {DemandPlanningTrace.DescribeRecipe(recipe.original)} amount={craftingTarget} flags={probe.Flags}");
			bool authorized = useDiagnosticAuthority
				? DemandPlannerAuthority.IsDiagnosticSupported(probe)
				: DemandPlannerAuthority.IsUiAuthorized(probe);
			if (!authorized) {
				trace?.Add(0, $"reject root: authority={DemandPlannerAuthority.Classify(probe)} diagnostic={useDiagnosticAuthority}");
				return false;
			}

			AvailableRecipeObjects planningAvailable = available.CloneForSimulation();
			if (!recipe.TryGetGraphGuidedCraftingInformation(craftingTarget, graph, planningAvailable, out CraftResult exactResult, trace, cancellationToken)) {
				trace?.Add(0, "planner failed to build a graph-guided craft result");
				return false;
			}

			if (!exactResult.WasAvailable || !ApplyCraftResult(exactResult, available.CloneForSimulation(), cancellationToken)) {
				trace?.Add(0, $"planner result rejected by replay: wasAvailable={exactResult.WasAvailable}");
				return false;
			}

			simulationResult = exactResult;
			AmountCrafted = craftingTarget;
			IsGraphBacked = true;
			return true;
		}

		private static int FindMaxCraftable(RecursiveRecipe recipe, int craftingTarget, AvailableRecipeObjects available, int mainResultItem, out CraftResult bestResult, CancellationToken cancellationToken) {
			bestResult = CraftResult.Default;

			int low = 0;
			int high = craftingTarget;

			while (low < high) {
				cancellationToken.ThrowIfCancellationRequested();

				int mid = low + (high - low + 1) / 2;
				if (TryPlanCrafts(recipe, mid, available.CloneForSimulation(), mainResultItem, out CraftResult craftResult, cancellationToken)) {
					low = mid;
					bestResult = craftResult;
				} else
					high = mid - 1;
			}

			return low;
		}

		private static bool TryPlanCrafts(RecursiveRecipe recipe, int amountToCraft, AvailableRecipeObjects available, int mainResultItem, out CraftResult craftResult, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			recipe.GetCraftingInformation(amountToCraft, out craftResult, available, mainResultItem, cancellationToken);

			if (!craftResult.WasAvailable)
				return false;

			return ApplyCraftResult(craftResult, available, cancellationToken);
		}

		private static bool ApplyCraftResult(CraftResult craftResult, AvailableRecipeObjects available, CancellationToken cancellationToken) {
			return ApplyCraftResult(craftResult, available, cancellationToken, trace: null);
		}

		private static bool ApplyCraftResult(CraftResult craftResult, AvailableRecipeObjects available, CancellationToken cancellationToken, ExactCraftingTrace trace) {
			foreach (RequiredMaterialInfo material in craftResult.requiredMaterials) {
				cancellationToken.ThrowIfCancellationRequested();

				int stack = material.Stack;
				if (stack <= 0)
					continue;

				trace?.Add(0, $"replay material {DescribeMaterialForTrace(material)} need={stack}");

				foreach (int item in material.GetValidItems()) {
					int before = available.GetIngredientQuantity(item);
					stack = available.UpdateIngredient(item, -stack);
					trace?.Add(0, $"  consume {DemandPlanningTrace.DescribeItem(item)} before={before} remainingNeed={stack}");

					if (stack <= 0)
						break;
				}

				if (stack > 0) {
					trace?.Add(0, $"replay material failed {DescribeMaterialForTrace(material)} missing={stack}");
					return false;
				}
			}

			foreach (ExcessItemInfo info in craftResult.excessResults) {
				cancellationToken.ThrowIfCancellationRequested();

				if (info.Stack > 0) {
					available.UpdateIngredient(info.type, info.Stack);
					trace?.Add(0, $"replay excess {DemandPlanningTrace.DescribeItem(info.type)} x{info.Stack}");
				}
			}

			return true;
		}

		private static string DescribeMaterialForTrace(RequiredMaterialInfo material) {
			if (!material.recipeGroup)
				return $"{DemandPlanningTrace.DescribeItem(material.itemOrGroupID)} x{material.Stack}";

			return $"group:{material.itemOrGroupID} x{material.Stack}";
		}
	}
}
