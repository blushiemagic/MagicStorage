using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class RecursiveRecipe {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				recipeToRecursiveRecipe.Clear();
			}
		}

		/// <summary>
		/// The base recipe object
		/// </summary>
		public readonly Recipe original;

		/// <summary>
		/// The tree representing the recipes used to recursive craft this recipe
		/// </summary>
		public readonly RecursionTree tree;

		internal static readonly ConditionalWeakTable<Recipe, RecursiveRecipe> recipeToRecursiveRecipe = new();

		public RecursiveRecipe(Recipe recipe) {
			original = recipe;
			tree = new RecursionTree(recipe);
		}

		public static void RecalculateAllRecursiveRecipes() {
			MagicCache.RecursiveRecipesUsingRecipeByIndex.Clear();
			MagicCache.DirectRecursiveRecipeParentsByRecipeIndex.Clear();

			NodePool.ClearNodes();

			foreach (var (_, recursive) in recipeToRecursiveRecipe) {
				recursive.tree.Reset();
				recursive.tree.CalculateTree();
			}
		}

		/// <summary>
		/// Returns a tree representing the recipes this recursive recipe will use and their expected crafted quantities
		/// </summary>
		/// <param name="amountToCraft">How many items are expected to be crafted.  Defaults to 1</param>
		/// <param name="available">An optional object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met</param>
		/// <param name="blockedSubrecipeIngredient">An optional item ID representing ingredient trees that should be ignored</param>
		public OrderedRecipeTree GetCraftingTree(int amountToCraft = 1, AvailableRecipeObjects available = null, int blockedSubrecipeIngredient = 0, CancellationToken cancellationToken = default) {
			return GetCraftingTree(amountToCraft, available, blockedSubrecipeIngredient, trace: null, cancellationToken);
		}

		internal OrderedRecipeTree GetCraftingTree(int amountToCraft, AvailableRecipeObjects available, int blockedSubrecipeIngredient, ExactCraftingTrace trace, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			// Is the main recipe not available?  If so, return an "empty tree"
			if (available?.CanUseRecipe(original) is false) {
				trace?.Add(0, $"reject root {DemandPlanningTrace.DescribeRecipe(original)}: station-condition");
				return new OrderedRecipeTree(null, 0);
			}

			int batchSize = original.createItem.stack;
			int batches = (int)Math.Ceiling(amountToCraft / (double)batchSize);

			HashSet<int> recursionStack = new();
			OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(original, 0, new SharedCounter(batches * batchSize)), 0);
			int depth = 0, maxDepth = 0;

			trace?.Add(0, $"build root {DemandPlanningTrace.DescribeRecipe(original)} amount={amountToCraft} batches={batches}");

			if (MagicStorageConfig.IsRecursionEnabled && (available is null || !available.creativeUnitPresent))
				ModifyCraftingTree(available, recursionStack, orderedTree, ref depth, ref maxDepth, batches, blockedSubrecipeIngredient, trace, cancellationToken);

			return orderedTree;
		}

		private void ModifyCraftingTree(AvailableRecipeObjects available, HashSet<int> recursionStack, OrderedRecipeTree root, ref int depth, ref int maxDepth, int parentBatches, int ignoreItem, ExactCraftingTrace trace, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			if (!MagicStorageConfig.IsRecursionInfinite && depth >= MagicStorageConfig.RecipeRecursionDepth) {
				trace?.Add(depth, $"stop exact tree at depth {depth}: max={MagicStorageConfig.RecipeRecursionDepth}");
				return;
			}

			// Safety check
			if (tree.Root is null) {
				trace?.Add(depth, "stop exact tree: source tree root is null");
				return;
			}

			// Check for infinitely recursive recipe branches (e.g. Wood -> Wood Platform -> Wood)
			// by recipe identity so alternate recipes with the same result are still considered.
			int recipeIndex = tree.originalRecipe.RecipeIndex;
			if (!recursionStack.Add(recipeIndex)) {
				trace?.Add(depth, $"stop exact tree {DemandPlanningTrace.DescribeRecipe(tree.originalRecipe)}: recursion stack");
				return;
			}

			depth++;

			if (depth > maxDepth)
				maxDepth = depth;

			foreach (RecipeIngredientInfo ingredient in tree.Root.info.ingredientTrees) {
				cancellationToken.ThrowIfCancellationRequested();

				Recipe sourceRecipe = ingredient.parent.sourceRecipe;
				if ((uint)ingredient.recipeIngredientIndex >= (uint)sourceRecipe.requiredItem.Count) {
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
					continue;
				}

				Item requiredItem = sourceRecipe.requiredItem[ingredient.recipeIngredientIndex];
				trace?.Add(depth, $"inspect ingredient {DemandPlanningTrace.DescribeItem(requiredItem.type)} x{requiredItem.stack * parentBatches} for {DemandPlanningTrace.DescribeRecipe(sourceRecipe)}");

				if (available is not null && available.isItemInfinite.Contains(requiredItem.type)) {
					// No recursion needed, go to next ingredient
					trace?.Add(depth, $"skip ingredient {DemandPlanningTrace.DescribeItem(requiredItem.type)}: infinite");
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
					continue;
				}

				int requiredPerCraft = requiredItem.stack;

				SharedCounter counter = new SharedCounter(requiredPerCraft * parentBatches);

				var possibleRecipes = ingredient.EnumerateValidRecipes(available, ignoreItem);
				
				bool anyRecipes = false;
				foreach (Recipe recipe in possibleRecipes) {
					cancellationToken.ThrowIfCancellationRequested();
					trace?.Add(depth, $"candidate exact child {DemandPlanningTrace.DescribeRecipe(recipe)} for {DemandPlanningTrace.DescribeItem(requiredItem.type)}");

					// Block recursion that would require the blocked item type
					if (ignoreItem > 0) {
						bool nextIngredient = false;
						foreach (Item item in recipe.requiredItem) {
							if (ignoreItem == item.type || CraftingGUI.RecipeGroupMatch(recipe, ignoreItem, item.type)) {
								nextIngredient = true;
								break;
							}
						}

						if (nextIngredient) {
							trace?.Add(depth, $"skip exact child {DemandPlanningTrace.DescribeRecipe(recipe)}: blocked ingredient {ignoreItem}");
							continue;
						}
					}

					anyRecipes = true;

					int batchSize = recipe.createItem.stack;
					int batches = (int)Math.Ceiling(requiredPerCraft / (double)batchSize * parentBatches);
					trace?.Add(depth, $"add exact child {DemandPlanningTrace.DescribeRecipe(recipe)} batches={batches}");

					OrderedRecipeTree orderedTree = new OrderedRecipeTree(new OrderedRecipeContext(recipe, depth, counter), ingredient.recipeIngredientIndex);
					root.Add(orderedTree);

					if (recipe.TryGetRecursiveRecipe(out var recursive))
						recursive.ModifyCraftingTree(available, recursionStack, orderedTree, ref depth, ref maxDepth, batches, ignoreItem, trace, cancellationToken);
				}

				if (!anyRecipes) {
					// Cannot recurse further, go to next ingredient
					trace?.Add(depth, $"no exact child recipes for {DemandPlanningTrace.DescribeItem(requiredItem.type)}");
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
				}
			}

			recursionStack.Remove(recipeIndex);

			depth--;
		}

		/// <summary>
		/// Returns a list of materials needed to craft this recursive recipe
		/// </summary>
		/// <param name="amountToCraft">How many items are expected to be crafted</param>
		/// <param name="result">A structure containing information about the recipes used</param>
		/// <param name="available">
		/// An optional object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met.<br/>
		/// This dictionary is used to trim the crafting tree before getting its required materials.
		/// </param>
		/// <param name="blockedSubrecipeIngredient">An optional item ID representing ingredient trees that should be ignored</param>
		public void GetCraftingInformation(int amountToCraft, out CraftResult result, AvailableRecipeObjects available = null, int blockedSubrecipeIngredient = 0, CancellationToken cancellationToken = default) {
			GetCraftingInformation(amountToCraft, out result, available, blockedSubrecipeIngredient, trace: null, cancellationToken);
		}

		internal void GetCraftingInformation(int amountToCraft, out CraftResult result, AvailableRecipeObjects available, int blockedSubrecipeIngredient, ExactCraftingTrace trace, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			var craftingTree = GetCraftingTree(amountToCraft, available, blockedSubrecipeIngredient, trace, cancellationToken);

			if (available is not null)
				craftingTree.TrimBranches(available, cancellationToken, trace);

			craftingTree.GetCraftingInformation(available, out result, cancellationToken, trace);
		}

		public bool TryGetGraphGuidedCraftingInformation(int amountToCraft, InventoryCraftabilityGraph graph, AvailableRecipeObjects available, out CraftResult result, CancellationToken cancellationToken = default)
			=> TryGetGraphGuidedCraftingInformation(amountToCraft, graph, available, out result, trace: null, cancellationToken);

		internal bool TryGetGraphGuidedCraftingInformation(int amountToCraft, InventoryCraftabilityGraph graph, AvailableRecipeObjects available, out CraftResult result, DemandPlanningTrace trace, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			result = default;

			if (graph is null || available is null || available.creativeUnitPresent) {
				trace?.Add(0, $"reject planner context: graphNull={graph is null}, availableNull={available is null}, creative={available?.creativeUnitPresent}");
				return false;
			}

			var ledger = new DemandPlanningLedger(available);
			var planningState = new DemandPlanningState(graph);
			HashSet<DemandPlanningFailureKey> failedIngredientPlans = new();
			HashSet<DemandPlanningGroupFailureKey> failedGroupPlans = new();
			if (!TryPlanGraphGuidedRecipe(original, amountToCraft, graph, ledger, depth: 0, planningState, allowAlternateSameResult: true, failedIngredientPlans, failedGroupPlans, trace, cancellationToken))
				return false;

			result = ledger.ToCraftResult();
			return result.WasAvailable;
		}

		private static bool TryPlanGraphGuidedRecipe(Recipe recipe, int amountToCraft, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, int depth, DemandPlanningState planningState, bool allowAlternateSameResult, HashSet<DemandPlanningFailureKey> failedIngredientPlans, HashSet<DemandPlanningGroupFailureKey> failedGroupPlans, DemandPlanningTrace trace, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			if (recipe is null || recipe.Disabled || recipe.createItem.stack <= 0 || !ledger.CanUseRecipe(recipe)) {
				trace?.Add(depth, $"reject recipe {DemandPlanningTrace.DescribeRecipe(recipe)}: null/disabled/output/station-condition");
				return false;
			}

			InventoryCraftabilityRecipeProbe probe = graph.ProbeRecipe(recipe);
			if (!CanUseProbeForDemandPlanning(probe, allowAlternateSameResult)) {
				trace?.Add(depth, $"reject recipe {DemandPlanningTrace.DescribeRecipe(recipe)}: probe flags={probe.Flags}, allowAlternate={allowAlternateSameResult}");
				return false;
			}

			if (depth > planningState.MaxDepth) {
				trace?.Add(depth, $"reject recipe {DemandPlanningTrace.DescribeRecipe(recipe)}: depth {depth}>{planningState.MaxDepth}");
				return false;
			}

			if (!planningState.TryEnterRecipe(recipe, depth, out DemandPlanningRecipeFrame frame)) {
				trace?.Add(depth, $"reject recipe {DemandPlanningTrace.DescribeRecipe(recipe)}: recursion/cycle gate");
				return false;
			}

			try {
				int batches = (int)Math.Ceiling(amountToCraft / (double)recipe.createItem.stack);
				if (batches <= 0) {
					trace?.Add(depth, $"reject recipe {DemandPlanningTrace.DescribeRecipe(recipe)}: batches={batches}");
					return false;
				}

				trace?.Add(depth, $"plan recipe {DemandPlanningTrace.DescribeRecipe(recipe)} amount={amountToCraft} batches={batches} flags={probe.Flags}");

				foreach (Item item in recipe.requiredItem) {
					cancellationToken.ThrowIfCancellationRequested();

					if (ledger.IsItemInfinite(item.type)) {
						trace?.Add(depth + 1, $"ingredient infinite {DemandPlanningTrace.DescribeItem(item.type)} x{item.stack * batches}");
						continue;
					}

					int requiredQuantity = item.stack * batches;
					if (TryGetRecipeGroup(recipe, item.type, out RecipeGroup group)) {
						trace?.Add(depth + 1, $"ingredient group {group.GetText()}({group.RegisteredId}) need={requiredQuantity}");
						if (!TryPlanGraphGuidedRecipeGroup(group, requiredQuantity, graph, ledger, depth + 1, planningState, failedIngredientPlans, failedGroupPlans, trace, cancellationToken))
							return false;
					} else {
						int consumed = ledger.ConsumeAvailableItem(item.type, requiredQuantity);
						int missingQuantity = requiredQuantity - consumed;
						trace?.Add(depth + 1, $"ingredient item {DemandPlanningTrace.DescribeItem(item.type)} need={requiredQuantity} direct={consumed} missing={missingQuantity}");

						if (missingQuantity <= 0)
							continue;

						if (!TryPlanGraphGuidedIngredient(item.type, missingQuantity, graph, ledger, depth + 1, planningState, failedIngredientPlans, failedGroupPlans, trace, cancellationToken))
							return false;

						if (!ledger.TryConsumeItem(item.type, missingQuantity)) {
							trace?.Add(depth + 1, $"consume planned item failed {DemandPlanningTrace.DescribeItem(item.type)} missing={missingQuantity}");
							return false;
						}
					}
				}

				ledger.RecordRecipeCraft(recipe, batches, depth);
				trace?.Add(depth, $"success recipe {DemandPlanningTrace.DescribeRecipe(recipe)} batches={batches}");
				return true;
			} finally {
				planningState.ExitRecipe(frame);
			}
		}

		private static bool TryPlanGraphGuidedIngredient(int itemType, int requiredQuantity, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, int depth, DemandPlanningState planningState, HashSet<DemandPlanningFailureKey> failedIngredientPlans, HashSet<DemandPlanningGroupFailureKey> failedGroupPlans, DemandPlanningTrace trace, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			var failureKey = DemandPlanningFailureKey.Create(itemType, requiredQuantity, depth, ledger, planningState);
			if (failedIngredientPlans.Contains(failureKey)) {
				trace?.Add(depth, $"skip ingredient {DemandPlanningTrace.DescribeItem(itemType)} need={requiredQuantity}: cached failure");
				return false;
			}

			const bool allowChildAlternateSameResult = true;
			IReadOnlyList<InventoryCraftabilityCandidate> candidates = graph.GetCandidates(itemType);
			List<InventoryCraftabilityCandidate> usableCandidates = null;
			List<InventoryCraftabilityCandidate> directlyAvailableCandidates = null;
			foreach (InventoryCraftabilityCandidate candidate in candidates) {
				cancellationToken.ThrowIfCancellationRequested();

				if (!IsUsablePlanningCandidate(candidate, graph, ledger, allowChildAlternateSameResult))
					continue;

				(usableCandidates ??= []).Add(candidate);
				if (AreRecipeIngredientsDirectlyAvailable(candidate.Recipe, ledger))
					(directlyAvailableCandidates ??= []).Add(candidate);
			}

			IEnumerable<InventoryCraftabilityCandidate> candidateSource = directlyAvailableCandidates ?? usableCandidates ?? [];
			trace?.Add(depth, $"plan ingredient {DemandPlanningTrace.DescribeItem(itemType)} need={requiredQuantity} candidates={candidates.Count} usable={usableCandidates?.Count ?? 0} directOnly={directlyAvailableCandidates is not null}");
			TraceCandidateFilters(trace, depth, candidates, graph, ledger, allowChildAlternateSameResult);

			bool triedCandidate = false;
			foreach (Recipe recipe in OrderGraphGuidedRecipes(candidateSource, graph, ledger, requiredQuantity)) {
				cancellationToken.ThrowIfCancellationRequested();

				if (recipe is null || recipe.Disabled || recipe.createItem.stack <= 0 || !ledger.CanUseRecipe(recipe)) {
					trace?.Add(depth, $"skip candidate {DemandPlanningTrace.DescribeRecipe(recipe)}: null/disabled/output/station-condition");
					continue;
				}

				InventoryCraftabilityRecipeProbe probe = graph.ProbeRecipe(recipe);
				if (!CanUseProbeForDemandPlanning(probe, allowChildAlternateSameResult)) {
					trace?.Add(depth, $"skip candidate {DemandPlanningTrace.DescribeRecipe(recipe)}: probe flags={probe.Flags}");
					continue;
				}

				int batches = (int)Math.Ceiling(requiredQuantity / (double)recipe.createItem.stack);
				if (batches <= 0) {
					trace?.Add(depth, $"skip candidate {DemandPlanningTrace.DescribeRecipe(recipe)}: batches={batches}");
					continue;
				}

				triedCandidate = true;
				trace?.Add(depth, $"try candidate {DemandPlanningTrace.DescribeRecipe(recipe)} batches={batches} flags={probe.Flags}");
				var checkpoint = ledger.CreateCheckpoint();
				if (TryPlanGraphGuidedRecipe(recipe, batches * recipe.createItem.stack, graph, ledger, depth, planningState, allowChildAlternateSameResult, failedIngredientPlans, failedGroupPlans, trace, cancellationToken)) {
					ledger.Commit(checkpoint);
					trace?.Add(depth, $"accept candidate {DemandPlanningTrace.DescribeRecipe(recipe)}");
					return true;
				}

				ledger.Rollback(checkpoint);
				trace?.Add(depth, $"reject candidate {DemandPlanningTrace.DescribeRecipe(recipe)}: branch failed");
			}

			if (triedCandidate) {
				failedIngredientPlans.Add(failureKey);
				trace?.Add(depth, $"fail ingredient {DemandPlanningTrace.DescribeItem(itemType)} need={requiredQuantity}: tried candidates failed");
			} else
				trace?.Add(depth, $"fail ingredient {DemandPlanningTrace.DescribeItem(itemType)} need={requiredQuantity}: no usable candidates");

			return false;
		}

		private static bool IsUsablePlanningCandidate(InventoryCraftabilityCandidate candidate, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, bool allowAlternateSameResult) {
			if (candidate?.Recipe is not { Disabled: false } recipe || recipe.createItem.stack <= 0 || !ledger.CanUseRecipe(recipe))
				return false;

			return CanUseProbeForDemandPlanning(graph.ProbeRecipe(recipe), allowAlternateSameResult);
		}

		private static void TraceCandidateFilters(DemandPlanningTrace trace, int depth, IEnumerable<InventoryCraftabilityCandidate> candidateSource, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, bool allowAlternateSameResult) {
			if (trace is null)
				return;

			foreach (InventoryCraftabilityCandidate candidate in candidateSource) {
				Recipe recipe = candidate?.Recipe;
				if (recipe is null) {
					trace.Add(depth, "candidate recipe:null rejected: null candidate");
					continue;
				}

				InventoryCraftabilityRecipeProbe probe = graph.ProbeRecipe(recipe);
				string status = IsUsablePlanningCandidate(candidate, graph, ledger, allowAlternateSameResult)
					? "usable"
					: GetCandidateRejectionReason(candidate, probe, ledger, allowAlternateSameResult);
				trace.Add(depth, $"candidate {DemandPlanningTrace.DescribeRecipe(recipe)} flags={probe.Flags} depth={candidate.Depth} output={candidate.OutputQuantity}: {status}");
			}
		}

		private static string GetCandidateRejectionReason(InventoryCraftabilityCandidate candidate, InventoryCraftabilityRecipeProbe probe, DemandPlanningLedger ledger, bool allowAlternateSameResult) {
			if (candidate?.Recipe is not { } recipe)
				return "rejected null";

			if (recipe.Disabled)
				return "rejected disabled";

			if (recipe.createItem.stack <= 0)
				return "rejected output";

			if (!ledger.CanUseRecipe(recipe))
				return $"rejected station-condition {DescribeMissingRecipeTiles(recipe, ledger)}";

			if (!CanUseProbeForDemandPlanning(probe, allowAlternateSameResult))
				return $"rejected probe allowAlternate={allowAlternateSameResult}";

			return "rejected unknown";
		}

		private static string DescribeMissingRecipeTiles(Recipe recipe, DemandPlanningLedger ledger) {
			List<int> missingTiles = null;
			foreach (int tile in recipe.requiredTile) {
				if (!ledger.IsTileAvailable(tile))
					(missingTiles ??= []).Add(tile);
			}

			return missingTiles is null ? "" : $"missingTiles={string.Join(",", missingTiles)}";
		}

		private static bool TryPlanGraphGuidedRecipeGroup(RecipeGroup group, int requiredQuantity, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, int depth, DemandPlanningState planningState, HashSet<DemandPlanningFailureKey> failedIngredientPlans, HashSet<DemandPlanningGroupFailureKey> failedGroupPlans, DemandPlanningTrace trace, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			if (group is null || requiredQuantity <= 0)
				return true;

			var failureKey = DemandPlanningGroupFailureKey.Create(group.RegisteredId, requiredQuantity, depth, ledger, planningState);
			if (failedGroupPlans.Contains(failureKey)) {
				trace?.Add(depth, $"skip group {group.GetText()}({group.RegisteredId}) need={requiredQuantity}: cached failure");
				return false;
			}

			int consumed = ledger.ConsumeAvailableRecipeGroup(group, requiredQuantity);
			int missingQuantity = requiredQuantity - consumed;
			trace?.Add(depth, $"plan group {group.GetText()}({group.RegisteredId}) need={requiredQuantity} direct={consumed} missing={missingQuantity}");
			if (missingQuantity <= 0)
				return true;

			foreach (int itemType in OrderRecipeGroupMembersForPlanning(group, graph, ledger, missingQuantity)) {
				cancellationToken.ThrowIfCancellationRequested();

				trace?.Add(depth, $"try group member {DemandPlanningTrace.DescribeItem(itemType)} need={missingQuantity}");
				var checkpoint = ledger.CreateCheckpoint();
				if (TryPlanGraphGuidedIngredient(itemType, missingQuantity, graph, ledger, depth, planningState, failedIngredientPlans, failedGroupPlans, trace, cancellationToken)
				&& ledger.TryConsumeItem(itemType, missingQuantity)) {
					ledger.Commit(checkpoint);
					trace?.Add(depth, $"accept group member {DemandPlanningTrace.DescribeItem(itemType)}");
					return true;
				}

				ledger.Rollback(checkpoint);
				trace?.Add(depth, $"reject group member {DemandPlanningTrace.DescribeItem(itemType)}");
			}

			failedGroupPlans.Add(failureKey);
			trace?.Add(depth, $"fail group {group.GetText()}({group.RegisteredId}) need={requiredQuantity}");

			return false;
		}

		private static IEnumerable<int> OrderRecipeGroupMembersForPlanning(RecipeGroup group, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, int requiredQuantity) {
			return group.ValidItems
				.Select(itemType => new DemandPlanningGroupMemberScore(itemType, graph, ledger, requiredQuantity))
				.Where(static score => score.HasCandidate)
				.OrderBy(static score => score.CyclicDependencyCount)
				.ThenBy(static score => score.GraphDepth)
				.ThenBy(static score => score.ScarceMaterialPressure)
				.ThenByDescending(static score => score.OutputQuantity)
				.ThenBy(static score => score.ItemType)
				.Select(static score => score.ItemType);
		}

		private static bool CanUseProbeForDemandPlanning(InventoryCraftabilityRecipeProbe probe, bool allowAlternateSameResult) {
			return DemandPlannerAuthority.IsChildCandidateSupported(probe, allowAlternateSameResult);
		}

		private static bool TryGetRecipeGroup(Recipe recipe, int itemType, out RecipeGroup group) {
			foreach (int groupID in recipe.acceptedGroups) {
				group = RecipeGroup.recipeGroups[groupID];
				if (group.ContainsItem(itemType))
					return true;
			}

			group = null;
			return false;
		}

		private OrderedRecipeTree GetGraphGuidedCraftingTree(int amountToCraft, InventoryCraftabilityGraph graph, AvailableRecipeObjects available, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			if (available.CanUseRecipe(original) is false || !DemandPlannerAuthority.IsTreePreviewSupported(graph.ProbeRecipe(original)))
				return new OrderedRecipeTree(null, 0);

			int batchSize = original.createItem.stack;
			int batches = (int)Math.Ceiling(amountToCraft / (double)batchSize);
			OrderedRecipeTree orderedTree = new(new OrderedRecipeContext(original, 0, new SharedCounter(batches * batchSize)), 0);

			if (MagicStorageConfig.IsRecursionEnabled && !available.creativeUnitPresent) {
				HashSet<int> recursionStack = new();
				BuildGraphGuidedCraftingTree(graph, available, recursionStack, orderedTree, depth: 0, parentBatches: batches, cancellationToken);
			}

			return orderedTree;
		}

		private void BuildGraphGuidedCraftingTree(InventoryCraftabilityGraph graph, AvailableRecipeObjects available, HashSet<int> recursionStack, OrderedRecipeTree root, int depth, int parentBatches, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			if (!MagicStorageConfig.IsRecursionInfinite && depth >= MagicStorageConfig.RecipeRecursionDepth)
				return;

			if (tree.Root is null)
				return;

			int recipeIndex = tree.originalRecipe.RecipeIndex;
			if (!recursionStack.Add(recipeIndex))
				return;

			int nextDepth = depth + 1;

			foreach (RecipeIngredientInfo ingredient in tree.Root.info.ingredientTrees) {
				cancellationToken.ThrowIfCancellationRequested();

				Recipe sourceRecipe = ingredient.parent.sourceRecipe;
				if ((uint)ingredient.recipeIngredientIndex >= (uint)sourceRecipe.requiredItem.Count) {
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
					continue;
				}

				Item requiredItem = sourceRecipe.requiredItem[ingredient.recipeIngredientIndex];
				if (available.isItemInfinite.Contains(requiredItem.type)) {
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
					continue;
				}

				int availableQuantity = available.GetTotalIngredientQuantity(sourceRecipe, requiredItem.type);
				int requiredQuantity = requiredItem.stack * parentBatches;
				if (availableQuantity >= requiredQuantity) {
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
					continue;
				}

				int remainingQuantity = requiredQuantity - availableQuantity;
				SharedCounter counter = new(remainingQuantity);

				bool anyRecipes = false;
				foreach (Recipe recipe in EnumerateGraphGuidedRecipes(ingredient, graph, available, requiredItem.type, cancellationToken)) {
					cancellationToken.ThrowIfCancellationRequested();

					anyRecipes = true;
					int batches = (int)Math.Ceiling(remainingQuantity / (double)recipe.createItem.stack);
					OrderedRecipeTree orderedTree = new(new OrderedRecipeContext(recipe, nextDepth, counter), ingredient.recipeIngredientIndex);
					root.Add(orderedTree);

					if (recipe.TryGetRecursiveRecipe(out var recursive))
						recursive.BuildGraphGuidedCraftingTree(graph, available, recursionStack, orderedTree, nextDepth, batches, cancellationToken);
				}

				if (!anyRecipes)
					root.Add(new OrderedRecipeTree(null, ingredient.recipeIngredientIndex));
			}

			recursionStack.Remove(recipeIndex);
		}

		private static IEnumerable<Recipe> EnumerateGraphGuidedRecipes(RecipeIngredientInfo ingredient, InventoryCraftabilityGraph graph, AvailableRecipeObjects available, int requiredItemType, CancellationToken cancellationToken) {
			List<Recipe> directRecipes = null;
			List<Recipe> candidateRecipes = null;

			foreach (Recipe recipe in ingredient.recipes) {
				cancellationToken.ThrowIfCancellationRequested();

				if (recipe is null || recipe.Disabled || !available.CanUseRecipe(recipe))
					continue;

				var probe = graph.ProbeRecipe(recipe);
				if (!DemandPlannerAuthority.IsTreePreviewSupported(probe))
					continue;

				if (AreRecipeIngredientsDirectlyAvailable(recipe, available)) {
					directRecipes ??= [];
					directRecipes.Add(recipe);
				} else {
					candidateRecipes ??= [];
					candidateRecipes.Add(recipe);
				}
			}

			if (directRecipes is not null) {
				foreach (Recipe recipe in OrderGraphGuidedRecipes(directRecipes, graph, requiredItemType))
					yield return recipe;
			}

			if (candidateRecipes is not null) {
				foreach (Recipe recipe in OrderGraphGuidedRecipes(candidateRecipes, graph, requiredItemType))
					yield return recipe;
			}
		}

		private static IEnumerable<Recipe> OrderGraphGuidedRecipes(IEnumerable<Recipe> recipes, InventoryCraftabilityGraph graph, int requiredItemType) {
			return recipes
				.Where(static recipe => recipe is not null && !recipe.Disabled && recipe.createItem.stack > 0)
				.Select(recipe => (Recipe: recipe, Probe: graph.ProbeRecipe(recipe)))
				.Where(static entry => DemandPlannerAuthority.IsTreePreviewSupported(entry.Probe))
				.OrderBy(static entry => entry.Probe.Candidate.Depth)
				.ThenByDescending(static entry => entry.Probe.Candidate.OutputQuantity)
				.ThenBy(static entry => entry.Recipe.RecipeIndex)
				.Select(static entry => entry.Recipe);
		}

		private static IEnumerable<Recipe> OrderGraphGuidedRecipes(IEnumerable<InventoryCraftabilityCandidate> candidates, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, int requiredQuantity) {
			return candidates
				.Select(candidate => new DemandPlanningCandidateScore(candidate.Recipe, graph.ProbeRecipe(candidate.Recipe), candidate, ledger, requiredQuantity))
				.OrderBy(static score => score.CyclicDependencyCount)
				.ThenByDescending(static score => score.DirectMaterialSatisfaction)
				.ThenBy(static score => score.VirtualDependencyCount)
				.ThenBy(static score => score.GraphDepth)
				.ThenBy(static score => score.RecipeGroupDependencyCount)
				.ThenBy(static score => score.ScarceMaterialPressure)
				.ThenByDescending(static score => score.OutputQuantity)
				.ThenBy(static score => score.Recipe.RecipeIndex)
				.Select(static score => score.Recipe);
		}

		private static bool AreRecipeIngredientsDirectlyAvailable(Recipe recipe, AvailableRecipeObjects available) {
			foreach (Item item in recipe.requiredItem) {
				bool usedRecipeGroup = false;
				ClampedArithmetic stack = item.stack;

				foreach (int groupID in recipe.acceptedGroups) {
					RecipeGroup group = RecipeGroup.recipeGroups[groupID];
					if (!group.ContainsItem(item.type))
						continue;

					foreach (int groupItem in group.ValidItems) {
						if (!available.TryGetIngredientQuantity(groupItem, out int count))
							continue;

						usedRecipeGroup = true;
						stack -= count;

						if (stack <= 0)
							goto checkNonRecipeGroup;
					}
				}

				checkNonRecipeGroup:

				if (!usedRecipeGroup && available.TryGetIngredientQuantity(item.type, out int directCount))
					stack -= directCount;

				if (stack > 0)
					return false;
			}

			return true;
		}

		private static bool AreRecipeIngredientsDirectlyAvailable(Recipe recipe, DemandPlanningLedger ledger) {
			foreach (Item item in recipe.requiredItem) {
				if (ledger.IsItemInfinite(item.type))
					continue;

				bool usedRecipeGroup = false;
				ClampedArithmetic stack = item.stack;

				foreach (int groupID in recipe.acceptedGroups) {
					RecipeGroup group = RecipeGroup.recipeGroups[groupID];
					if (!group.ContainsItem(item.type))
						continue;

					foreach (int groupItem in group.ValidItems) {
						int count = ledger.GetIngredientQuantity(groupItem);
						if (count <= 0)
							continue;

						usedRecipeGroup = true;
						stack -= count;

						if (stack <= 0)
							goto checkNonRecipeGroup;
					}
				}

				checkNonRecipeGroup:

				if (!usedRecipeGroup)
					stack -= ledger.GetIngredientQuantity(item.type);

				if (stack > 0)
					return false;
			}

			return true;
		}

		private readonly struct DemandPlanningCandidateScore {
			public Recipe Recipe { get; }
			public InventoryCraftabilityRecipeProbe Probe { get; }
			public int DirectMaterialSatisfaction { get; }
			public int VirtualDependencyCount { get; }
			public int GraphDepth { get; }
			public int RecipeGroupDependencyCount { get; }
			public int CyclicDependencyCount { get; }
			public int ScarceMaterialPressure { get; }
			public int OutputQuantity { get; }

			public DemandPlanningCandidateScore(Recipe recipe, InventoryCraftabilityRecipeProbe probe, InventoryCraftabilityCandidate candidate, DemandPlanningLedger ledger, int requiredQuantity) {
				Recipe = recipe;
				Probe = probe;
				GraphDepth = candidate.Depth;
				OutputQuantity = candidate.OutputQuantity;
				int batches = recipe.createItem.stack > 0
					? (int)Math.Ceiling(requiredQuantity / (double)recipe.createItem.stack)
					: 0;

				long directSatisfied = 0;
				long directRequired = 0;
				long scarcePressure = 0;
				int virtualDependencies = 0;
				int recipeGroupDependencies = 0;

				foreach (var dependency in candidate.Dependencies) {
					if (dependency.UsesRecipeGroup)
						recipeGroupDependencies++;

					int ledgerQuantity = GetLedgerDependencyQuantity(dependency, ledger);
					if (dependency.AvailableQuantity > ledgerQuantity)
						virtualDependencies++;

					long requiredLong = (long)dependency.RequiredQuantityPerBatch * batches;
					int required = requiredLong >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)requiredLong);
					if (required <= 0)
						continue;

					int direct = Math.Min(ledgerQuantity, required);
					directRequired += required;
					directSatisfied += direct;
					scarcePressure += required - direct;
				}

				DirectMaterialSatisfaction = directRequired <= 0 ? int.MaxValue : ClampToInt(directSatisfied * 1000 / directRequired);
				VirtualDependencyCount = virtualDependencies;
				RecipeGroupDependencyCount = recipeGroupDependencies;
				CyclicDependencyCount = (probe.Flags & InventoryCraftabilityProbeFlags.CyclicDependencyRegion) != 0 ? 1 : 0;
				ScarceMaterialPressure = ClampToInt(scarcePressure);
			}
		}

		private static int ClampToInt(long value) {
			if (value >= int.MaxValue)
				return int.MaxValue;

			if (value <= int.MinValue)
				return int.MinValue;

			return (int)value;
		}

		private static int GetLedgerDependencyQuantity(InventoryCraftabilityDependency dependency, DemandPlanningLedger ledger) {
			if (!dependency.UsesRecipeGroup)
				return ledger.GetIngredientQuantity(dependency.IngredientType);

			long quantity = 0;
			RecipeGroup group = RecipeGroup.recipeGroups[dependency.RecipeGroupId];
			foreach (int itemType in group.ValidItems) {
				quantity += ledger.GetIngredientQuantity(itemType);

				if (quantity >= int.MaxValue)
					return int.MaxValue;
			}

			return (int)quantity;
		}

		private sealed class DemandPlanningState {
			private readonly InventoryCraftabilityGraph graph;
			private readonly HashSet<int> recursionStack = new();
			private readonly Dictionary<int, int> cyclicComponentVisits = new();
			private int recursionFingerprint;

			public int MaxDepth { get; }

			public DemandPlanningState(InventoryCraftabilityGraph graph) {
				this.graph = graph;
				MaxDepth = graph?.MaxDepth ?? 0;
			}

			public bool TryEnterRecipe(Recipe recipe, int depth, out DemandPlanningRecipeFrame frame) {
				frame = default;

				bool isCyclic = graph.TryGetStronglyConnectedComponent(recipe, out RecipeStronglyConnectedComponent component) && component.IsCyclic;
				if (isCyclic && depth > MaxDepth)
					return false;

				bool addedToStack = recursionStack.Add(recipe.RecipeIndex);
				if (!addedToStack && !isCyclic)
					return false;
				if (addedToStack)
					recursionFingerprint ^= GetRecursionStackEntryHash(recipe.RecipeIndex);

				int componentId = isCyclic ? component.Id : -1;
				if (isCyclic) {
					int visits = cyclicComponentVisits.GetValueOrDefault(componentId);
					int maxVisits = Math.Max(1, MaxDepth + 1);
					if (visits >= maxVisits) {
						if (addedToStack) {
							recursionStack.Remove(recipe.RecipeIndex);
							recursionFingerprint ^= GetRecursionStackEntryHash(recipe.RecipeIndex);
						}

						return false;
					}

					if (visits > 0)
						recursionFingerprint ^= GetCyclicComponentEntryHash(componentId, visits);

					cyclicComponentVisits[componentId] = visits + 1;
					recursionFingerprint ^= GetCyclicComponentEntryHash(componentId, visits + 1);
				}

				frame = new DemandPlanningRecipeFrame(recipe.RecipeIndex, addedToStack, componentId);
				return true;
			}

			public void ExitRecipe(DemandPlanningRecipeFrame frame) {
				if (frame.CyclicComponentId >= 0) {
					int visits = cyclicComponentVisits[frame.CyclicComponentId] - 1;
					recursionFingerprint ^= GetCyclicComponentEntryHash(frame.CyclicComponentId, visits + 1);

					if (visits > 0)
						cyclicComponentVisits[frame.CyclicComponentId] = visits;
					else
						cyclicComponentVisits.Remove(frame.CyclicComponentId);

					if (visits > 0)
						recursionFingerprint ^= GetCyclicComponentEntryHash(frame.CyclicComponentId, visits);
				}

				if (frame.AddedToStack) {
					recursionStack.Remove(frame.RecipeIndex);
					recursionFingerprint ^= GetRecursionStackEntryHash(frame.RecipeIndex);
				}
			}

			public int GetRecursionFingerprint() => recursionFingerprint;

			private static int GetRecursionStackEntryHash(int recipeIndex)
				=> HashCode.Combine(1, recipeIndex);

			private static int GetCyclicComponentEntryHash(int componentId, int visits)
				=> HashCode.Combine(2, componentId, visits);
		}

		private readonly struct DemandPlanningRecipeFrame {
			public int RecipeIndex { get; }
			public bool AddedToStack { get; }
			public int CyclicComponentId { get; }

			public DemandPlanningRecipeFrame(int recipeIndex, bool addedToStack, int cyclicComponentId) {
				RecipeIndex = recipeIndex;
				AddedToStack = addedToStack;
				CyclicComponentId = cyclicComponentId;
			}
		}

		private readonly struct DemandPlanningFailureKey : IEquatable<DemandPlanningFailureKey> {
			private readonly int itemType;
			private readonly int requiredQuantity;
			private readonly int depth;
			private readonly int ledgerFingerprint;
			private readonly int recursionStackHash;

			private DemandPlanningFailureKey(int itemType, int requiredQuantity, int depth, int ledgerFingerprint, int recursionStackHash) {
				this.itemType = itemType;
				this.requiredQuantity = requiredQuantity;
				this.depth = depth;
				this.ledgerFingerprint = ledgerFingerprint;
				this.recursionStackHash = recursionStackHash;
			}

			public static DemandPlanningFailureKey Create(int itemType, int requiredQuantity, int depth, DemandPlanningLedger ledger, DemandPlanningState planningState)
				=> new(itemType, requiredQuantity, depth, ledger.GetInventoryFingerprint(), planningState.GetRecursionFingerprint());

			public bool Equals(DemandPlanningFailureKey other)
				=> itemType == other.itemType
				&& requiredQuantity == other.requiredQuantity
				&& depth == other.depth
				&& ledgerFingerprint == other.ledgerFingerprint
				&& recursionStackHash == other.recursionStackHash;

			public override bool Equals(object obj) => obj is DemandPlanningFailureKey other && Equals(other);

			public override int GetHashCode() => HashCode.Combine(itemType, requiredQuantity, depth, ledgerFingerprint, recursionStackHash);
		}

		private readonly struct DemandPlanningGroupMemberScore {
			private const bool AllowChildAlternateSameResult = true;

			public int ItemType { get; }
			public bool HasCandidate { get; }
			public int CyclicDependencyCount { get; }
			public int GraphDepth { get; }
			public int ScarceMaterialPressure { get; }
			public int OutputQuantity { get; }

			public DemandPlanningGroupMemberScore(int itemType, InventoryCraftabilityGraph graph, DemandPlanningLedger ledger, int requiredQuantity) {
				ItemType = itemType;
				HasCandidate = false;
				CyclicDependencyCount = int.MaxValue;
				GraphDepth = int.MaxValue;
				ScarceMaterialPressure = int.MaxValue;
				OutputQuantity = 0;

				foreach (InventoryCraftabilityCandidate candidate in graph.GetCandidates(itemType)) {
					Recipe recipe = candidate.Recipe;
					if (recipe is null || recipe.Disabled || recipe.createItem.stack <= 0)
						continue;

					InventoryCraftabilityRecipeProbe probe = graph.ProbeRecipe(recipe);
					if (!CanUseProbeForDemandPlanning(probe, AllowChildAlternateSameResult))
						continue;

					var score = new DemandPlanningCandidateScore(recipe, probe, candidate, ledger, requiredQuantity);
					if (!HasCandidate || IsBetter(score, CyclicDependencyCount, GraphDepth, ScarceMaterialPressure, OutputQuantity)) {
						HasCandidate = true;
						CyclicDependencyCount = score.CyclicDependencyCount;
						GraphDepth = score.GraphDepth;
						ScarceMaterialPressure = score.ScarceMaterialPressure;
						OutputQuantity = score.OutputQuantity;
					}
				}
			}

			private static bool IsBetter(DemandPlanningCandidateScore score, int cyclicDependencyCount, int graphDepth, int scarceMaterialPressure, int outputQuantity) {
				if (score.CyclicDependencyCount != cyclicDependencyCount)
					return score.CyclicDependencyCount < cyclicDependencyCount;

				if (score.GraphDepth != graphDepth)
					return score.GraphDepth < graphDepth;

				if (score.ScarceMaterialPressure != scarceMaterialPressure)
					return score.ScarceMaterialPressure < scarceMaterialPressure;

				return score.OutputQuantity > outputQuantity;
			}
		}

		private readonly struct DemandPlanningGroupFailureKey : IEquatable<DemandPlanningGroupFailureKey> {
			private readonly int groupId;
			private readonly int requiredQuantity;
			private readonly int depth;
			private readonly int ledgerFingerprint;
			private readonly int recursionStackHash;

			private DemandPlanningGroupFailureKey(int groupId, int requiredQuantity, int depth, int ledgerFingerprint, int recursionStackHash) {
				this.groupId = groupId;
				this.requiredQuantity = requiredQuantity;
				this.depth = depth;
				this.ledgerFingerprint = ledgerFingerprint;
				this.recursionStackHash = recursionStackHash;
			}

			public static DemandPlanningGroupFailureKey Create(int groupId, int requiredQuantity, int depth, DemandPlanningLedger ledger, DemandPlanningState planningState)
				=> new(groupId, requiredQuantity, depth, ledger.GetInventoryFingerprint(), planningState.GetRecursionFingerprint());

			public bool Equals(DemandPlanningGroupFailureKey other)
				=> groupId == other.groupId
				&& requiredQuantity == other.requiredQuantity
				&& depth == other.depth
				&& ledgerFingerprint == other.ledgerFingerprint
				&& recursionStackHash == other.recursionStackHash;

			public override bool Equals(object obj) => obj is DemandPlanningGroupFailureKey other && Equals(other);

			public override int GetHashCode() => HashCode.Combine(groupId, requiredQuantity, depth, ledgerFingerprint, recursionStackHash);
		}

		/// <summary>
		/// Iterate's through this recursive recipe's crafting tree and calculates the maximum amount of this recipe that can be crafted
		/// </summary>
		/// <param name="available">
		/// An object indicating which item types are available and their quantities, which crafting stations are available and which recipe conditions have been met.<br/>
		/// <b>NOTE:</b> the contents of this object may be modified by the time this method call returns
		/// </param>
		/// <returns>The maximum amount of this recipe that can be crafted, or 0 if the information in <paramref name="available"/> could not satisfy any recipes</returns>
		/// <exception cref="ArgumentNullException"/>
		public int GetMaxCraftable(AvailableRecipeObjects available, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(available);
			cancellationToken.ThrowIfCancellationRequested();

			if (available is not null && (available.creativeUnitPresent || available.isItemInfinite.Contains(original.createItem.type)))
				return Item.CommonMaxStack;

			var simulation = new CraftingSimulation();
			simulation.SimulateCrafts(this, Item.CommonMaxStack, available, cancellationToken: cancellationToken);
			return simulation.AmountCrafted;
		}
	}
}
