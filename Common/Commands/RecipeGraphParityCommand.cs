using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Commands {
	internal class RecipeGraphParityCommand : ModCommand {
		private const int DefaultLimit = 100;
		private const int MaxSafeLimit = 1000;
		private const int DefaultDepth = 5;
		private const int DefaultTimeBudgetMs = 20000;
		private const int ForceTimeBudgetMs = 60000;
		private const int MaxReportedSamples = 8;
		private const int MaxReportedTraceSamplesPerKind = 2;
		private const string BudgetArgumentPrefix = "budget=";
		private const string ForceArgument = "force";
		private const string FullModeArgument = "full";
		private const string DiagnosticModeArgument = "diagnostic";
		private const string TraceArgument = "trace";

		public override string Command => "msgraphparity";

		public override CommandType Type => CommandType.Chat;

		public override string Usage => this.GetUsageText("[limit] [depth] [full|diagnostic] [trace] [force] [budget=ms]");

		public override string Description => "Compare the recursive craftability graph against exact crafting simulation for the current crafting UI snapshot.";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length > 7) {
				caller.Reply($"Usage: {Usage}", Color.Red);
				return;
			}

			int limit = DefaultLimit;
			if (args.Length >= 1 && (!int.TryParse(args[0], out limit) || limit <= 0)) {
				caller.Reply("limit must be a positive integer.", Color.Red);
				return;
			}

			int depth = DefaultDepth;
			if (args.Length >= 2 && (!int.TryParse(args[1], out depth) || depth <= 0)) {
				caller.Reply("depth must be a positive integer.", Color.Red);
				return;
			}

			bool fullMode = false;
			bool diagnosticMode = false;
			bool tracePlanner = false;
			bool force = false;
			int? requestedTimeBudgetMs = null;
			for (int i = 2; i < args.Length; i++) {
				if (string.Equals(args[i], FullModeArgument, StringComparison.OrdinalIgnoreCase))
					fullMode = true;
				else if (string.Equals(args[i], DiagnosticModeArgument, StringComparison.OrdinalIgnoreCase))
					diagnosticMode = true;
				else if (string.Equals(args[i], TraceArgument, StringComparison.OrdinalIgnoreCase))
					tracePlanner = true;
				else if (string.Equals(args[i], ForceArgument, StringComparison.OrdinalIgnoreCase))
					force = true;
				else if (args[i].StartsWith(BudgetArgumentPrefix, StringComparison.OrdinalIgnoreCase)) {
					string value = args[i][BudgetArgumentPrefix.Length..];
					if (!int.TryParse(value, out int timeBudgetMs) || timeBudgetMs < 0) {
						caller.Reply("budget must be a non-negative integer in milliseconds, for example budget=5000. Use budget=0 to disable the time budget.", Color.Red);
						return;
					}

					requestedTimeBudgetMs = timeBudgetMs;
				}
				else {
					caller.Reply($"Unknown argument \"{args[i]}\". Use \"{FullModeArgument}\" for full exact statistics, \"{DiagnosticModeArgument}\" for diagnostic planner authority, \"{TraceArgument}\" for mismatch planner traces, \"{ForceArgument}\" for large samples, or {BudgetArgumentPrefix}ms for a time budget.", Color.Red);
					return;
				}
			}

			if (tracePlanner && !diagnosticMode) {
				caller.Reply($"Planner trace is diagnostic-only; use {Command} {limit} {depth} {FullModeArgument} {DiagnosticModeArgument} {TraceArgument}.", Color.Red);
				return;
			}

			if (!force && limit > MaxSafeLimit && fullMode) {
				caller.Reply($"Full graph parity exact simulation runs on the main thread. Limit {limit} is above the safe cap {MaxSafeLimit}; use {Command} {limit} {depth} {FullModeArgument} {ForceArgument} if you intentionally want a long freeze-risk run.", Color.OrangeRed);
				return;
			}

			if (!MagicStorageConfig.IsRecursionEnabled) {
				caller.Reply("Recursive crafting is disabled; graph parity has nothing to compare.", Color.Red);
				return;
			}

			if (MagicCache.EnabledRecipes is not { Length: > 0 } recipes) {
				caller.Reply("Recipe cache is not ready.", Color.Red);
				return;
			}

			try {
				int timeBudgetMs = requestedTimeBudgetMs ?? (force ? ForceTimeBudgetMs : DefaultTimeBudgetMs);
				RunParity(caller, recipes, limit, depth, fullMode, diagnosticMode, tracePlanner, timeBudgetMs);
			} catch (OperationCanceledException) {
				caller.Reply("Graph parity run was cancelled.", Color.Orange);
			} catch (Exception ex) {
				Mod.Logger.Error("Recipe graph parity harness failed", ex);
				caller.Reply($"Graph parity failed: {ex.GetType().Name}: {ex.Message}", Color.Red);
			}
		}

		private static void RunParity(CommandCaller caller, Recipe[] recipes, int limit, int depth, bool fullMode, bool diagnosticMode, bool tracePlanner, int timeBudgetMs) {
			AvailableRecipeObjects available = CraftingGUI.GetCurrentInventory(cloneIfBlockEmpty: true);
			var snapshot = CraftingGUI.GetCurrentInventoryDebugSnapshot();
			var context = CraftingGUI.CreateCurrentCraftingSimulationContextForDebug(1);
			using CancellationTokenSource budgetCancellation = timeBudgetMs > 0 ? new CancellationTokenSource(timeBudgetMs) : null;
			CancellationToken cancellationToken = budgetCancellation?.Token ?? default;

			var watch = Stopwatch.StartNew();
			InventoryCraftabilityGraph graph = InventoryCraftabilityGraph.Build(available, recipes, depth, cancellationToken);
			long graphMs = watch.ElapsedMilliseconds;

			int checkedRecipes = 0;
			int skippedRecipes = 0;
			int exactAvailable = 0;
			int exactChecked = 0;
			int exactSkipped = 0;
			int graphCandidates = 0;
			int graphMissingExactAvailable = 0;
			int directAuthorityMismatch = 0;
			int ambiguousCandidateExactFail = 0;
			int plannerChecked = 0;
			int plannerAvailable = 0;
			int plannerMissedExact = 0;
			int plannerFalsePositive = 0;
			int plannerSupersedesExact = 0;
			int plannerAlternateChecked = 0;
			int plannerAlternateAvailable = 0;
			int plannerAlternateMissedExact = 0;
			int plannerAlternateFalsePositive = 0;
			int plannerAlternateSupersedesExact = 0;
			int plannerGroupChecked = 0;
			int plannerGroupAvailable = 0;
			int plannerGroupMissedExact = 0;
			int plannerGroupFalsePositive = 0;
			int plannerGroupSupersedesExact = 0;
			int plannerVirtualChecked = 0;
			int plannerVirtualAvailable = 0;
			int plannerVirtualMissedExact = 0;
			int plannerVirtualFalsePositive = 0;
			int plannerVirtualSupersedesExact = 0;
			int plannerCyclicChecked = 0;
			int plannerCyclicAvailable = 0;
			int plannerCyclicMissedExact = 0;
			int plannerCyclicFalsePositive = 0;
			int plannerCyclicSupersedesExact = 0;
			int plannerSkippedAmbiguous = 0;
			int plannerSkippedAlternate = 0;
			int plannerSkippedRecipeGroup = 0;
			int plannerSkippedVirtual = 0;
			int plannerSkippedCyclic = 0;
			int authorityDirect = 0;
			int authorityRecipeGroup = 0;
			int authorityVirtual = 0;
			int authorityCyclic = 0;
			int authorityRootAlternate = 0;
			int authorityUnsupported = 0;
			List<string> missingSamples = new();
			List<string> directMismatchSamples = new();
			List<string> plannerMissSamples = new();
			List<string> plannerFalsePositiveSamples = new();
			List<string> plannerSupersedesExactSamples = new();
			List<string> plannerTraceSamples = new();
			int plannerMissTraceSamples = 0;
			int plannerSupersedesExactTraceSamples = 0;

			watch.Restart();
			long exactSimulationTicks = 0;
			long plannerTicks = 0;
			bool partial = false;

			foreach (Recipe recipe in recipes) {
				if (checkedRecipes >= limit)
					break;

				if (timeBudgetMs > 0 && watch.ElapsedMilliseconds >= timeBudgetMs) {
					partial = true;
					break;
				}

				if (!recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe)) {
					skippedRecipes++;
					continue;
				}

				checkedRecipes++;

				InventoryCraftabilityRecipeProbe probe = graph.ProbeRecipe(recipe);
				CountAuthorityClass(probe, ref authorityDirect, ref authorityRecipeGroup, ref authorityVirtual, ref authorityCyclic, ref authorityRootAlternate, ref authorityUnsupported);
				if (probe.HasCandidate) {
					graphCandidates++;
					if (!ShouldRunPlanner(probe, diagnosticMode))
						CountPlannerSkippedProbe(probe, ref plannerSkippedAmbiguous, ref plannerSkippedAlternate, ref plannerSkippedRecipeGroup, ref plannerSkippedVirtual, ref plannerSkippedCyclic);
				}

				bool shouldRunPlanner = ShouldRunPlanner(probe, diagnosticMode);
				bool shouldRunExact = fullMode || !probe.HasCandidate || probe.IsDirectRecipeAuthority || shouldRunPlanner;
				if (!shouldRunExact) {
					exactSkipped++;
					continue;
				}

				exactChecked++;

				var simulation = new CraftingSimulation();
				long exactStart = Stopwatch.GetTimestamp();
				try {
					simulation.SimulateCrafts(recursiveRecipe, 1, available, context, cancellationToken);
				} catch (OperationCanceledException) {
					partial = true;
					break;
				} finally {
					exactSimulationTicks += Stopwatch.GetTimestamp() - exactStart;
				}
				bool exactCanCraft = simulation.AmountCrafted > 0;
				if (exactCanCraft)
					exactAvailable++;

				bool plannerCanCraft = false;
				if (shouldRunPlanner) {
					CountPlannerProbeByFlag(probe, ref plannerAlternateChecked, ref plannerGroupChecked, ref plannerVirtualChecked, ref plannerCyclicChecked);

					plannerChecked++;
					var graphSimulation = new CraftingSimulation();
					long plannerStart = Stopwatch.GetTimestamp();
					try {
						plannerCanCraft = diagnosticMode
							? graphSimulation.TryPlanCraftsWithGraphForDiagnostics(recursiveRecipe, 1, available, graph, context, cancellationToken)
							: graphSimulation.TryPlanCraftsWithGraph(recursiveRecipe, 1, available, graph, context, cancellationToken);
					} catch (OperationCanceledException) {
						partial = true;
						break;
					} finally {
						plannerTicks += Stopwatch.GetTimestamp() - plannerStart;
					}
					if (plannerCanCraft) {
						plannerAvailable++;
						CountPlannerProbeByFlag(probe, ref plannerAlternateAvailable, ref plannerGroupAvailable, ref plannerVirtualAvailable, ref plannerCyclicAvailable);
					}

					if (exactCanCraft && !plannerCanCraft) {
						plannerMissedExact++;
						CountPlannerProbeByFlag(probe, ref plannerAlternateMissedExact, ref plannerGroupMissedExact, ref plannerVirtualMissedExact, ref plannerCyclicMissedExact);

						if (AddSample(plannerMissSamples, "planner-miss", recipe, simulation.AmountCrafted, graphSimulation.AmountCrafted, probe)
						&& tracePlanner
						&& plannerMissTraceSamples++ < MaxReportedTraceSamplesPerKind)
							AddTraceSample(plannerTraceSamples, "planner-miss", recipe, recursiveRecipe, simulation, available, graph, context, cancellationToken);
					} else if (!exactCanCraft && plannerCanCraft) {
						plannerSupersedesExact++;
						CountPlannerProbeByFlag(probe, ref plannerAlternateSupersedesExact, ref plannerGroupSupersedesExact, ref plannerVirtualSupersedesExact, ref plannerCyclicSupersedesExact);

						if (AddSample(plannerSupersedesExactSamples, "planner-supersedes-exact", recipe, simulation.AmountCrafted, graphSimulation.AmountCrafted, probe)
						&& tracePlanner
						&& plannerSupersedesExactTraceSamples++ < MaxReportedTraceSamplesPerKind)
							AddTraceSample(plannerTraceSamples, "planner-supersedes-exact", recipe, recursiveRecipe, simulation, available, graph, context, cancellationToken);
					}
				}

				if (exactCanCraft && !probe.HasCandidate) {
					graphMissingExactAvailable++;
					AddSample(missingSamples, "missing", recipe, simulation.AmountCrafted, plannerAmountCrafted: null, probe);
					continue;
				}

				if (!exactCanCraft && probe.IsDirectRecipeAuthority) {
					directAuthorityMismatch++;
					AddSample(directMismatchSamples, "direct-mismatch", recipe, simulation.AmountCrafted, plannerAmountCrafted: null, probe);
					continue;
				}

				if (!exactCanCraft && probe.HasCandidate)
					ambiguousCandidateExactFail++;
			}

			long simulationMs = watch.ElapsedMilliseconds;
			long exactMs = (long)(exactSimulationTicks * 1000.0 / Stopwatch.Frequency);
			long plannerMs = (long)(plannerTicks * 1000.0 / Stopwatch.Frequency);
			string mode = fullMode && diagnosticMode ? "full+diagnostic" : fullMode ? "full" : diagnosticMode ? "diagnostic" : "strict";
			string partialText = partial ? " partial" : "";
			string budgetText = timeBudgetMs > 0 ? $"{timeBudgetMs}ms" : "off";
			string summary = $"Graph parity ({mode}{partialText}) checked {checkedRecipes}/{limit} recursive recipes, skipped {skippedRecipes}. "
				+ $"exactChecked={exactChecked}, exactSkipped={exactSkipped}, exactAvailable={exactAvailable}, graphCandidates={graphCandidates}, missingExact={graphMissingExactAvailable}, "
				+ $"directMismatch={directAuthorityMismatch}, candidateExactFail={ambiguousCandidateExactFail}, "
				+ $"plannerChecked={plannerChecked}, plannerAvailable={plannerAvailable}, plannerMissedExact={plannerMissedExact}, plannerFalsePositive={plannerFalsePositive}, plannerSupersedesExact={plannerSupersedesExact}. "
				+ $"plannerAlternateChecked={plannerAlternateChecked}, plannerAlternateAvailable={plannerAlternateAvailable}, plannerAlternateMissedExact={plannerAlternateMissedExact}, plannerAlternateFalsePositive={plannerAlternateFalsePositive}, plannerAlternateSupersedesExact={plannerAlternateSupersedesExact}. "
				+ $"plannerGroupChecked={plannerGroupChecked}, plannerGroupAvailable={plannerGroupAvailable}, plannerGroupMissedExact={plannerGroupMissedExact}, plannerGroupFalsePositive={plannerGroupFalsePositive}, plannerGroupSupersedesExact={plannerGroupSupersedesExact}. "
				+ $"plannerVirtualChecked={plannerVirtualChecked}, plannerVirtualAvailable={plannerVirtualAvailable}, plannerVirtualMissedExact={plannerVirtualMissedExact}, plannerVirtualFalsePositive={plannerVirtualFalsePositive}, plannerVirtualSupersedesExact={plannerVirtualSupersedesExact}. "
				+ $"plannerCyclicChecked={plannerCyclicChecked}, plannerCyclicAvailable={plannerCyclicAvailable}, plannerCyclicMissedExact={plannerCyclicMissedExact}, plannerCyclicFalsePositive={plannerCyclicFalsePositive}, plannerCyclicSupersedesExact={plannerCyclicSupersedesExact}. "
				+ $"plannerSkippedAmbiguous={plannerSkippedAmbiguous}, plannerSkippedAlternate={plannerSkippedAlternate}, plannerSkippedRecipeGroup={plannerSkippedRecipeGroup}, "
				+ $"plannerSkippedVirtual={plannerSkippedVirtual}, plannerSkippedCyclic={plannerSkippedCyclic}. "
				+ $"authorityClasses direct={authorityDirect}, group={authorityRecipeGroup}, virtual={authorityVirtual}, cyclic={authorityCyclic}, rootAlternate={authorityRootAlternate}, unsupported={authorityUnsupported}. "
				+ $"authority virtual={DemandPlannerAuthority.IsVirtualDependencyUiAuthorized}, cyclic={DemandPlannerAuthority.IsCyclicBoundedUiAuthorized}, rootAlternate={DemandPlannerAuthority.IsRootAlternateUiAuthorized}. "
				+ $"time graph={graphMs}ms, exact={exactMs}ms, planner={plannerMs}ms, totalCompare={simulationMs}ms, budget={budgetText}, depth={depth}. "
				+ $"snapshot ui={snapshot.CraftingUiOpen}, complete={snapshot.HasCompleteData}, refreshing={snapshot.CurrentlyRefreshing}, "
				+ $"itemTypes={snapshot.ItemTypes}, totalStack={snapshot.TotalStack}, stations={snapshot.StationCount}, "
				+ $"blocked={snapshot.BlockedItems}, infiniteItems={snapshot.InfiniteItems}, creative={snapshot.CreativeUnitPresent}.";

			Color color = partial ? Color.Orange : graphMissingExactAvailable == 0 && directAuthorityMismatch == 0 && plannerMissedExact == 0 && plannerFalsePositive == 0 ? Color.LightGreen : Color.OrangeRed;
			caller.Reply(summary, color);
			ModContent.GetInstance<MagicStorageMod>().Logger.Info(summary);

			ReportSamples(caller, missingSamples);
			ReportSamples(caller, directMismatchSamples);
			ReportSamples(caller, plannerMissSamples);
			ReportSamples(caller, plannerFalsePositiveSamples);
			ReportSamples(caller, plannerSupersedesExactSamples);
			ReportSamples(caller, plannerTraceSamples);
		}

		private static void CountPlannerSkippedProbe(
			InventoryCraftabilityRecipeProbe probe,
			ref int ambiguous,
			ref int alternate,
			ref int recipeGroup,
			ref int virtualDependency,
			ref int cyclic
		) {
			ambiguous++;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.AlternateSameResultRecipe) != 0)
				alternate++;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.RecipeGroupDependency) != 0)
				recipeGroup++;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.VirtualDependency) != 0)
				virtualDependency++;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.CyclicDependencyRegion) != 0)
				cyclic++;
		}

		private static bool ShouldRunPlanner(InventoryCraftabilityRecipeProbe probe, bool diagnosticMode)
			=> diagnosticMode ? DemandPlannerAuthority.IsDiagnosticSupported(probe) : DemandPlannerAuthority.IsUiAuthorized(probe);

		private static void CountAuthorityClass(
			InventoryCraftabilityRecipeProbe probe,
			ref int direct,
			ref int recipeGroup,
			ref int virtualDependency,
			ref int cyclic,
			ref int rootAlternate,
			ref int unsupported
		) {
			switch (DemandPlannerAuthority.Classify(probe)) {
				case DemandPlannerAuthorityClass.Direct:
					direct++;
					break;
				case DemandPlannerAuthorityClass.RecipeGroup:
					recipeGroup++;
					break;
				case DemandPlannerAuthorityClass.VirtualDependency:
					virtualDependency++;
					break;
				case DemandPlannerAuthorityClass.CyclicBounded:
					cyclic++;
					break;
				case DemandPlannerAuthorityClass.RootAlternate:
					rootAlternate++;
					break;
				default:
					unsupported++;
					break;
			}
		}

		private static void CountPlannerProbeByFlag(
			InventoryCraftabilityRecipeProbe probe,
			ref int alternate,
			ref int recipeGroup,
			ref int virtualDependency,
			ref int cyclic
		) {
			if ((probe.Flags & InventoryCraftabilityProbeFlags.AlternateSameResultRecipe) != 0)
				alternate++;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.RecipeGroupDependency) != 0)
				recipeGroup++;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.VirtualDependency) != 0)
				virtualDependency++;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.CyclicDependencyRegion) != 0)
				cyclic++;
		}

		private static bool AddSample(List<string> samples, string kind, Recipe recipe, int exactAmountCrafted, int? plannerAmountCrafted, InventoryCraftabilityRecipeProbe probe) {
			if (samples.Count >= MaxReportedSamples)
				return false;

			Item result = recipe.createItem;
			string itemName = result?.Name ?? $"item:{result?.type ?? 0}";
			string plannerText = plannerAmountCrafted is { } plannerAmount
				? $", planner={plannerAmount}"
				: "";
			samples.Add($"[{kind}] #{recipe.RecipeIndex} {itemName} x{result?.stack ?? 0}: exact={exactAmountCrafted}{plannerText}, graph={probe.HasCandidate}, flags={probe.Flags}");
			return true;
		}

		private static void AddTraceSample(List<string> samples, string kind, Recipe recipe, RecursiveRecipe recursiveRecipe, CraftingSimulation exactSimulation, AvailableRecipeObjects available, InventoryCraftabilityGraph graph, CraftingSimulationContext context, CancellationToken cancellationToken) {
			var exactTrace = new ExactCraftingTrace();
			var tracedExactSimulation = new CraftingSimulation();
			try {
				tracedExactSimulation.TryTraceExactCrafts(recursiveRecipe, 1, available, context, exactTrace, cancellationToken);

				var trace = new DemandPlanningTrace();
				var plannerSimulation = new CraftingSimulation();
				plannerSimulation.TryPlanCraftsWithGraphForDiagnostics(recursiveRecipe, 1, available, graph, context, trace, cancellationToken);

				samples.Add($"[planner-trace:{kind}] {DemandPlanningTrace.DescribeRecipe(recipe)} exact={exactSimulation.AmountCrafted}, planner={plannerSimulation.AmountCrafted}");
				AddSimulationTraceLines(samples, "exact", exactSimulation);
				AddSimulationTraceLines(samples, "planner", plannerSimulation);

				if (!exactTrace.HasEvents)
					samples.Add("[exact-trace:event] <none>");
				else {
					foreach (string line in exactTrace.EnumerateLines())
						samples.Add($"[exact-trace:event] {line}");
				}

				if (!trace.HasEvents) {
					samples.Add("[planner-trace:event] <none>");
					return;
				}

				foreach (string line in trace.EnumerateLines())
					samples.Add($"[planner-trace:event] {line}");
			} catch (OperationCanceledException) {
				samples.Add($"[planner-trace:{kind}] cancelled by parity budget");
			}
		}

		private static void AddSimulationTraceLines(List<string> samples, string label, CraftingSimulation simulation) {
			string usedRecipes = string.Join(" -> ", simulation.UsedRecipes
				.Take(16)
				.Select(DemandPlanningTrace.DescribeRecipe));
			samples.Add($"[planner-trace:{label}:recipes] {(string.IsNullOrEmpty(usedRecipes) ? "<none>" : usedRecipes)}");

			string materials = string.Join(", ", simulation.RequiredMaterials
				.Where(static material => material.Stack > 0)
				.Take(16)
				.Select(DescribeMaterial));
			samples.Add($"[planner-trace:{label}:materials] {(string.IsNullOrEmpty(materials) ? "<none>" : materials)}");

			string excess = string.Join(", ", simulation.ExcessResults
				.Where(static info => info.Stack > 0)
				.Take(16)
				.Select(static info => $"{DemandPlanningTrace.DescribeItem(info.type)} x{info.Stack} prefix={info.prefix}"));
			samples.Add($"[planner-trace:{label}:excess] {(string.IsNullOrEmpty(excess) ? "<none>" : excess)}");
		}

		private static string DescribeMaterial(RequiredMaterialInfo material) {
			if (!material.recipeGroup)
				return $"{DemandPlanningTrace.DescribeItem(material.itemOrGroupID)} x{material.Stack}";

			string groupName = RecipeGroup.recipeGroups.TryGetValue(material.itemOrGroupID, out RecipeGroup group)
				? group.GetText().ToString()
				: $"group:{material.itemOrGroupID}";
			return $"{groupName}({material.itemOrGroupID}) x{material.Stack}";
		}

		private static void ReportSamples(CommandCaller caller, List<string> samples) {
			foreach (string sample in samples) {
				caller.Reply(sample, Color.LightGray);
				ModContent.GetInstance<MagicStorageMod>().Logger.Warn(sample);
			}
		}
	}
}
