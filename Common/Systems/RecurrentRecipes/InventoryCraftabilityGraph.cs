using System;
using System.Collections.Generic;
using System.Threading;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	/// <summary>
	/// A read-only craftability capacity graph built from one inventory snapshot.
	/// </summary>
	public sealed class InventoryCraftabilityGraph {
		private static readonly object ingredientIndexCacheLock = new();
		private static IReadOnlyList<Recipe> cachedIngredientIndexRecipes;
		private static RecipeIngredientIndex cachedIngredientIndex;

		private readonly Dictionary<int, int> capacities;
		private readonly Dictionary<int, InventoryCraftabilityNode> nodes;
		private readonly HashSet<Recipe> candidateRecipes;
		private readonly Dictionary<Recipe, InventoryCraftabilityRecipeProbe> recipeProbesByRecipe;
		private readonly RecipeIngredientIndex recipeIngredientIndex;

		/// <summary>
		/// Gets whether missing recipe probes can be treated as authoritative negative results.
		/// </summary>
		public bool CanRejectMissingRecipes { get; }

		/// <summary>
		/// Gets the maximum recursive propagation depth represented by this graph snapshot.
		/// </summary>
		public int MaxDepth { get; }

		private InventoryCraftabilityGraph(
			Dictionary<int, int> capacities,
			Dictionary<int, InventoryCraftabilityNode> nodes,
			HashSet<Recipe> candidateRecipes,
			Dictionary<Recipe, InventoryCraftabilityRecipeProbe> recipeProbesByRecipe,
			RecipeIngredientIndex recipeIngredientIndex,
			bool canRejectMissingRecipes,
			int maxDepth
		) {
			this.capacities = capacities;
			this.nodes = nodes;
			this.candidateRecipes = candidateRecipes;
			this.recipeProbesByRecipe = recipeProbesByRecipe;
			this.recipeIngredientIndex = recipeIngredientIndex;
			CanRejectMissingRecipes = canRejectMissingRecipes;
			MaxDepth = Math.Max(0, maxDepth);
		}

		/// <summary>
		/// Clears cached recipe lookup data used by graph construction.
		/// </summary>
		public static void ClearRecipeIndexCache() {
			lock (ingredientIndexCacheLock) {
				cachedIngredientIndexRecipes = null;
				cachedIngredientIndex = null;
			}
		}

		/// <summary>
		/// Builds a depth-limited item capacity graph from the provided inventory and recipe set.
		/// </summary>
		/// <param name="available">The inventory, station, condition, and infinite-item snapshot.</param>
		/// <param name="recipes">The recipes that can contribute virtual item capacities.</param>
		/// <param name="maxDepth">The maximum number of recipe propagation layers.</param>
		/// <param name="cancellationToken">The cancellation token for refresh-thread callers.</param>
		public static InventoryCraftabilityGraph Build(AvailableRecipeObjects available, IReadOnlyList<Recipe> recipes, int maxDepth, CancellationToken cancellationToken = default)
			=> BuildCore(available, recipes, maxDepth, initialFrontierItemTypes: null, seedRecipes: null, canRejectMissingRecipes: true, cancellationToken);

		/// <summary>
		/// Builds a depth-limited item capacity graph whose propagation starts only from the supplied changed item types.
		/// </summary>
		/// <param name="available">The inventory, station, condition, and infinite-item snapshot.</param>
		/// <param name="recipes">The recipes that can contribute virtual item capacities.</param>
		/// <param name="maxDepth">The maximum number of recipe propagation layers.</param>
		/// <param name="initialFrontierItemTypes">The item types whose changed capacity should seed propagation.</param>
		/// <param name="cancellationToken">The cancellation token for refresh-thread callers.</param>
		public static InventoryCraftabilityGraph BuildFocused(AvailableRecipeObjects available, IReadOnlyList<Recipe> recipes, int maxDepth, IEnumerable<int> initialFrontierItemTypes, CancellationToken cancellationToken = default)
			=> BuildFocused(available, recipes, maxDepth, initialFrontierItemTypes, seedRecipes: null, cancellationToken);

		/// <summary>
		/// Builds a depth-limited focused graph whose first layer also tries a known set of affected recipes.
		/// </summary>
		/// <param name="available">The inventory, station, condition, and infinite-item snapshot.</param>
		/// <param name="recipes">The recipes that can contribute virtual item capacities.</param>
		/// <param name="maxDepth">The maximum number of recipe propagation layers.</param>
		/// <param name="initialFrontierItemTypes">The item types whose changed capacity should seed propagation.</param>
		/// <param name="seedRecipes">Recipes already known to be affected by the current refresh.</param>
		/// <param name="cancellationToken">The cancellation token for refresh-thread callers.</param>
		public static InventoryCraftabilityGraph BuildFocused(AvailableRecipeObjects available, IReadOnlyList<Recipe> recipes, int maxDepth, IEnumerable<int> initialFrontierItemTypes, IEnumerable<Recipe> seedRecipes, CancellationToken cancellationToken = default)
			=> BuildCore(available, recipes, maxDepth, initialFrontierItemTypes, seedRecipes, canRejectMissingRecipes: false, cancellationToken);

		/// <summary>
		/// Rebuilds the graph from a new direct inventory snapshot while reusing old candidates outside the changed item closure.
		/// </summary>
		/// <param name="available">The new inventory, station, condition, and infinite-item snapshot.</param>
		/// <param name="recipes">The recipes that can contribute virtual item capacities.</param>
		/// <param name="maxDepth">The maximum number of recipe propagation layers.</param>
		/// <param name="changedItemTypes">The item types whose direct quantities changed since this graph was built.</param>
		/// <param name="seedRecipes">Recipes already known to be affected by the current refresh.</param>
		/// <param name="cancellationToken">The cancellation token for refresh-thread callers.</param>
		public InventoryCraftabilityGraph UpdateIncremental(AvailableRecipeObjects available, IReadOnlyList<Recipe> recipes, int maxDepth, IEnumerable<int> changedItemTypes, IEnumerable<Recipe> seedRecipes, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(available);
			ArgumentNullException.ThrowIfNull(recipes);

			if (!CanRejectMissingRecipes)
				return Build(available, recipes, maxDepth, cancellationToken);

			RecipeIngredientIndex recipeIngredientIndex = GetOrCreateIngredientIndex(recipes);
			HashSet<int> affectedItemTypes = CollectAffectedItemTypes(changedItemTypes, seedRecipes, recipeIngredientIndex, maxDepth, cancellationToken);
			if (affectedItemTypes.Count <= 0)
				return Build(available, recipes, maxDepth, cancellationToken);

			Dictionary<int, int> capacities = new();
			Dictionary<int, int> directCapacities = new();
			Dictionary<int, int> minimumDepths = new();
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType = new();
			HashSet<Recipe> candidateRecipeSet = new(ReferenceEqualityComparer.Instance);

			PopulateDirectCapacities(available, capacities, directCapacities, minimumDepths, cancellationToken);
			PreserveUnaffectedCandidates(affectedItemTypes, capacities, directCapacities, minimumDepths, candidatesByItemType, candidateRecipeSet, cancellationToken);
			PropagateCandidates(
				available,
				recipeIngredientIndex,
				capacities,
				minimumDepths,
				candidatesByItemType,
				candidateRecipeSet,
				frontier: affectedItemTypes,
				seedRecipes,
				maxDepth,
				restrictToResultItemTypes: affectedItemTypes,
				includeRecipesWithoutIngredientsOnFirstLayer: false,
				solvedRecipes: null,
				cancellationToken);

			return CreateGraph(capacities, directCapacities, minimumDepths, candidatesByItemType, candidateRecipeSet, recipeIngredientIndex, canRejectMissingRecipes: true, maxDepth);
		}

		private static InventoryCraftabilityGraph BuildCore(
			AvailableRecipeObjects available,
			IReadOnlyList<Recipe> recipes,
			int maxDepth,
			IEnumerable<int> initialFrontierItemTypes,
			IEnumerable<Recipe> seedRecipes,
			bool canRejectMissingRecipes,
			CancellationToken cancellationToken
		) {
			ArgumentNullException.ThrowIfNull(available);
			ArgumentNullException.ThrowIfNull(recipes);

			Dictionary<int, int> capacities = new();
			Dictionary<int, int> directCapacities = new();
			Dictionary<int, int> minimumDepths = new();
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType = new();
			HashSet<Recipe> candidateRecipeSet = new(ReferenceEqualityComparer.Instance);
			HashSet<Recipe> solvedRecipes = new(ReferenceEqualityComparer.Instance);
			RecipeIngredientIndex recipeIngredientIndex = GetOrCreateIngredientIndex(recipes);
			HashSet<int> frontier = initialFrontierItemTypes is null ? new() : new(initialFrontierItemTypes);
			bool useFullInventoryFrontier = initialFrontierItemTypes is null;

			PopulateDirectCapacities(available, capacities, directCapacities, minimumDepths, cancellationToken);

			if (useFullInventoryFrontier) {
				foreach (int type in capacities.Keys)
					frontier.Add(type);
			}

			if (available.creativeUnitPresent)
				return CreateGraph(capacities, directCapacities, minimumDepths, candidatesByItemType, candidateRecipeSet, recipeIngredientIndex, canRejectMissingRecipes, maxDepth);

			if (useFullInventoryFrontier)
				SolveAcyclicStronglyConnectedComponentDag(
					available,
					recipeIngredientIndex,
					capacities,
					minimumDepths,
					candidatesByItemType,
					candidateRecipeSet,
					solvedRecipes,
					frontier,
					maxDepth,
					cancellationToken);

			PropagateCandidates(
				available,
				recipeIngredientIndex,
				capacities,
				minimumDepths,
				candidatesByItemType,
				candidateRecipeSet,
				frontier,
				seedRecipes,
				maxDepth,
				restrictToResultItemTypes: null,
				includeRecipesWithoutIngredientsOnFirstLayer: useFullInventoryFrontier,
				solvedRecipes,
				cancellationToken);

			return CreateGraph(capacities, directCapacities, minimumDepths, candidatesByItemType, candidateRecipeSet, recipeIngredientIndex, canRejectMissingRecipes, maxDepth);
		}

		private static void PopulateDirectCapacities(AvailableRecipeObjects available, Dictionary<int, int> capacities, Dictionary<int, int> directCapacities, Dictionary<int, int> minimumDepths, CancellationToken cancellationToken) {
			foreach (var (type, quantity) in available.EnumerateInventory()) {
				cancellationToken.ThrowIfCancellationRequested();

				if (quantity <= 0)
					continue;

				AddCapacity(capacities, type, quantity);
				AddCapacity(directCapacities, type, quantity);
				minimumDepths[type] = 0;
			}

			foreach (int type in available.isItemInfinite) {
				cancellationToken.ThrowIfCancellationRequested();
				capacities[type] = int.MaxValue;
				directCapacities[type] = int.MaxValue;
				minimumDepths[type] = 0;
			}
		}

		private static void PropagateCandidates(
			AvailableRecipeObjects available,
			RecipeIngredientIndex recipeIngredientIndex,
			Dictionary<int, int> capacities,
			Dictionary<int, int> minimumDepths,
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType,
			HashSet<Recipe> candidateRecipeSet,
			HashSet<int> frontier,
			IEnumerable<Recipe> seedRecipes,
			int maxDepth,
			HashSet<int> restrictToResultItemTypes,
			bool includeRecipesWithoutIngredientsOnFirstLayer,
			HashSet<Recipe> solvedRecipes,
			CancellationToken cancellationToken
		) {
			if (available.creativeUnitPresent)
				return;

			int depthLimit = MagicStorageConfig.IsRecursionInfinite ? Math.Max(1, maxDepth) : Math.Max(0, maxDepth);
			for (int depth = 0; depth < depthLimit; depth++) {
				cancellationToken.ThrowIfCancellationRequested();

				List<Recipe> candidateRecipes = GetCandidateRecipes(
					frontier,
					recipeIngredientIndex,
					includeRecipesWithoutIngredients: includeRecipesWithoutIngredientsOnFirstLayer && depth == 0,
					seedRecipes: depth == 0 ? seedRecipes : null);
				if (candidateRecipes.Count <= 0)
					break;

				Dictionary<int, int> additions = new();
				Dictionary<int, List<InventoryCraftabilityCandidate>> layerCandidates = new();
				int candidateDepth = depth + 1;

				foreach (Recipe recipe in candidateRecipes) {
					cancellationToken.ThrowIfCancellationRequested();

					if (recipe is null || recipe.Disabled || solvedRecipes?.Contains(recipe) is true || !available.CanUseRecipe(recipe))
						continue;

					if (restrictToResultItemTypes is not null && !restrictToResultItemTypes.Contains(recipe.createItem.type))
						continue;

					if (!TryGetPossibleOutput(recipe, capacities, out int outputQuantity, out int craftableBatches, out var dependencies))
						continue;

					AddCapacity(additions, recipe.createItem.type, outputQuantity);
					AddCandidate(layerCandidates, recipe.createItem.type, new InventoryCraftabilityCandidate(recipe, candidateDepth, craftableBatches, outputQuantity, dependencies));
				}

				if (additions.Count <= 0)
					break;

				bool changed = false;
				HashSet<int> nextFrontier = new();
				foreach (var (type, quantity) in additions) {
					int oldQuantity = GetCapacity(capacities, type);
					AddCapacity(capacities, type, quantity);

					if (GetCapacity(capacities, type) != oldQuantity) {
						changed = true;
						nextFrontier.Add(type);
						if (!minimumDepths.TryGetValue(type, out int oldDepth) || candidateDepth < oldDepth)
							minimumDepths[type] = candidateDepth;
					}
				}

				foreach (var (type, candidates) in layerCandidates) {
					foreach (var candidate in candidates) {
						AddCandidate(candidatesByItemType, type, candidate);
						candidateRecipeSet.Add(candidate.Recipe);
					}
				}

				if (!changed)
					break;

				frontier = nextFrontier;
			}
		}

		private static void SolveAcyclicStronglyConnectedComponentDag(
			AvailableRecipeObjects available,
			RecipeIngredientIndex recipeIngredientIndex,
			Dictionary<int, int> capacities,
			Dictionary<int, int> minimumDepths,
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType,
			HashSet<Recipe> candidateRecipeSet,
			HashSet<Recipe> solvedRecipes,
			HashSet<int> frontier,
			int maxDepth,
			CancellationToken cancellationToken
		) {
			int depthLimit = MagicStorageConfig.IsRecursionInfinite ? Math.Max(1, maxDepth) : Math.Max(0, maxDepth);
			if (depthLimit <= 0)
				return;

			foreach (RecipeStronglyConnectedComponent component in recipeIngredientIndex.ComponentsInTopologicalOrder) {
				cancellationToken.ThrowIfCancellationRequested();

				if (component.IsCyclic)
					continue;

				foreach (Recipe recipe in component.Recipes) {
					cancellationToken.ThrowIfCancellationRequested();

					if (recipe is null || recipe.Disabled || solvedRecipes.Contains(recipe) || !available.CanUseRecipe(recipe))
						continue;

					if (!TryGetPossibleOutput(recipe, capacities, out int outputQuantity, out int craftableBatches, out var dependencies))
						continue;

					if (!TryGetCandidateDepth(dependencies, minimumDepths, depthLimit, out int candidateDepth))
						continue;

					int oldQuantity = GetCapacity(capacities, recipe.createItem.type);
					AddCapacity(capacities, recipe.createItem.type, outputQuantity);
					if (GetCapacity(capacities, recipe.createItem.type) != oldQuantity)
						frontier.Add(recipe.createItem.type);

					if (!minimumDepths.TryGetValue(recipe.createItem.type, out int oldDepth) || candidateDepth < oldDepth)
						minimumDepths[recipe.createItem.type] = candidateDepth;

					AddCandidate(candidatesByItemType, recipe.createItem.type, new InventoryCraftabilityCandidate(recipe, candidateDepth, craftableBatches, outputQuantity, dependencies));
					candidateRecipeSet.Add(recipe);
					solvedRecipes.Add(recipe);
				}
			}
		}

		private static bool TryGetCandidateDepth(IReadOnlyList<InventoryCraftabilityDependency> dependencies, Dictionary<int, int> minimumDepths, int depthLimit, out int candidateDepth) {
			candidateDepth = 1;

			foreach (var dependency in dependencies) {
				int dependencyDepth = GetDependencyMinimumDepth(dependency, minimumDepths);
				if (dependencyDepth < 0)
					return false;

				int depth = dependencyDepth + 1;
				if (depth > candidateDepth)
					candidateDepth = depth;
			}

			return candidateDepth <= depthLimit;
		}

		private static int GetDependencyMinimumDepth(InventoryCraftabilityDependency dependency, Dictionary<int, int> minimumDepths) {
			if (!dependency.UsesRecipeGroup)
				return minimumDepths.TryGetValue(dependency.IngredientType, out int depth) ? depth : -1;

			int minDepth = int.MaxValue;
			RecipeGroup group = RecipeGroup.recipeGroups[dependency.RecipeGroupId];
			foreach (int groupItem in group.ValidItems) {
				if (minimumDepths.TryGetValue(groupItem, out int depth) && depth < minDepth)
					minDepth = depth;
			}

			return minDepth == int.MaxValue ? -1 : minDepth;
		}

		private void PreserveUnaffectedCandidates(
			HashSet<int> affectedItemTypes,
			Dictionary<int, int> capacities,
			Dictionary<int, int> directCapacities,
			Dictionary<int, int> minimumDepths,
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType,
			HashSet<Recipe> candidateRecipeSet,
			CancellationToken cancellationToken
		) {
			foreach (var node in nodes.Values) {
				cancellationToken.ThrowIfCancellationRequested();

				foreach (var candidate in node.Candidates) {
					cancellationToken.ThrowIfCancellationRequested();

					if (!TryRecomputePreservedCandidate(candidate, affectedItemTypes, directCapacities, out var preservedCandidate))
						continue;

					AddCapacity(capacities, preservedCandidate.Recipe.createItem.type, preservedCandidate.OutputQuantity);
					if (!minimumDepths.TryGetValue(preservedCandidate.Recipe.createItem.type, out int oldDepth) || preservedCandidate.Depth < oldDepth)
						minimumDepths[preservedCandidate.Recipe.createItem.type] = preservedCandidate.Depth;

					AddCandidate(candidatesByItemType, preservedCandidate.Recipe.createItem.type, preservedCandidate);
					candidateRecipeSet.Add(preservedCandidate.Recipe);
				}
			}
		}

		private bool TryRecomputePreservedCandidate(
			InventoryCraftabilityCandidate candidate,
			HashSet<int> affectedItemTypes,
			Dictionary<int, int> capacities,
			out InventoryCraftabilityCandidate preservedCandidate
		) {
			preservedCandidate = null;

			if (candidate.Recipe is null
			|| candidate.Recipe.Disabled
			|| affectedItemTypes.Contains(candidate.Recipe.createItem.type)
			|| CandidateTouchesAffectedItem(candidate, affectedItemTypes)
			|| CandidateUsesVirtualCapacity(candidate))
				return false;

			if (!TryGetPossibleOutput(candidate.Recipe, capacities, out int outputQuantity, out int craftableBatches, out var dependencies))
				return false;

			preservedCandidate = new InventoryCraftabilityCandidate(candidate.Recipe, candidate.Depth, craftableBatches, outputQuantity, dependencies);
			return true;
		}

		private static bool CandidateTouchesAffectedItem(InventoryCraftabilityCandidate candidate, HashSet<int> affectedItemTypes) {
			foreach (var dependency in candidate.Dependencies) {
				if (affectedItemTypes.Contains(dependency.IngredientType))
					return true;

				if (!dependency.UsesRecipeGroup)
					continue;

				RecipeGroup group = RecipeGroup.recipeGroups[dependency.RecipeGroupId];
				foreach (int groupItem in group.ValidItems) {
					if (affectedItemTypes.Contains(groupItem))
						return true;
				}
			}

			return false;
		}

		private bool CandidateUsesVirtualCapacity(InventoryCraftabilityCandidate candidate) {
			foreach (var dependency in candidate.Dependencies) {
				if (dependency.UsesRecipeGroup) {
					long directQuantity = 0;
					RecipeGroup group = RecipeGroup.recipeGroups[dependency.RecipeGroupId];
					foreach (int groupItem in group.ValidItems) {
						if (nodes.TryGetValue(groupItem, out var groupNode))
							directQuantity += groupNode.DirectQuantity;

						if (directQuantity >= dependency.AvailableQuantity)
							break;
					}

					if (directQuantity < dependency.AvailableQuantity)
						return true;

					continue;
				}

				if (!nodes.TryGetValue(dependency.IngredientType, out var dependencyNode)
				|| dependencyNode.DirectQuantity < dependency.AvailableQuantity)
					return true;
			}

			return false;
		}

		private static HashSet<int> CollectAffectedItemTypes(IEnumerable<int> changedItemTypes, IEnumerable<Recipe> seedRecipes, RecipeIngredientIndex recipeIngredientIndex, int maxDepth, CancellationToken cancellationToken) {
			HashSet<int> affected = new();
			HashSet<int> expandedStronglyConnectedComponents = new();
			Queue<(int itemType, int depth)> queue = new();

			if (changedItemTypes is not null) {
				foreach (int itemType in changedItemTypes) {
					if (itemType <= 0 || !affected.Add(itemType))
						continue;

					queue.Enqueue((itemType, 0));
				}
			}

			if (seedRecipes is not null) {
				foreach (Recipe recipe in seedRecipes) {
					cancellationToken.ThrowIfCancellationRequested();

					if (recipe is null || recipe.Disabled)
						continue;

					int resultType = recipe.createItem.type;
					if (resultType > 0 && affected.Add(resultType))
						queue.Enqueue((resultType, 0));

					EnqueueStronglyConnectedRecipeResults(recipe, recipeIngredientIndex, affected, expandedStronglyConnectedComponents, queue, depth: 0, cancellationToken);
				}
			}

			int depthLimit = MagicStorageConfig.IsRecursionInfinite ? Math.Max(1, maxDepth) : Math.Max(0, maxDepth);
			while (queue.Count > 0) {
				cancellationToken.ThrowIfCancellationRequested();

				var (itemType, depth) = queue.Dequeue();
				if (depth >= depthLimit)
					continue;

				if (!recipeIngredientIndex.RecipesByIngredientType.TryGetValue(itemType, out var recipes))
					continue;

				foreach (Recipe recipe in recipes) {
					cancellationToken.ThrowIfCancellationRequested();

					if (recipe is null || recipe.Disabled)
						continue;

					int resultType = recipe.createItem.type;
					if (resultType <= 0 || !affected.Add(resultType))
						EnqueueStronglyConnectedRecipeResults(recipe, recipeIngredientIndex, affected, expandedStronglyConnectedComponents, queue, depth + 1, cancellationToken);
					else {
						queue.Enqueue((resultType, depth + 1));
						EnqueueStronglyConnectedRecipeResults(recipe, recipeIngredientIndex, affected, expandedStronglyConnectedComponents, queue, depth + 1, cancellationToken);
					}
				}
			}

			return affected;
		}

		private static void EnqueueStronglyConnectedRecipeResults(
			Recipe recipe,
			RecipeIngredientIndex recipeIngredientIndex,
			HashSet<int> affected,
			HashSet<int> expandedStronglyConnectedComponents,
			Queue<(int itemType, int depth)> queue,
			int depth,
			CancellationToken cancellationToken
		) {
			if (!recipeIngredientIndex.TryGetStronglyConnectedComponent(recipe, out var component) || !component.IsCyclic)
				return;

			if (!expandedStronglyConnectedComponents.Add(component.Id))
				return;

			foreach (Recipe componentRecipe in component.Recipes) {
				cancellationToken.ThrowIfCancellationRequested();

				if (componentRecipe is null || componentRecipe.Disabled)
					continue;

				int resultType = componentRecipe.createItem.type;
				if (resultType > 0 && affected.Add(resultType))
					queue.Enqueue((resultType, depth));
			}
		}

		/// <summary>
		/// Gets the direct or virtually craftable quantity for an item type in this graph.
		/// </summary>
		public int GetCraftableQuantity(int itemType) => GetCapacity(capacities, itemType);

		/// <summary>
		/// Gets whether an item type has any direct or virtually craftable quantity in this graph.
		/// </summary>
		public bool CanReach(int itemType) => GetCraftableQuantity(itemType) > 0;

		/// <summary>
		/// Gets whether this graph observed at least one recipe candidate that can produce the item type.
		/// </summary>
		public bool CanProduce(int itemType) => GetCandidates(itemType).Count > 0;

		/// <summary>
		/// Gets whether this graph observed the specified recipe as a production candidate for its result item type.
		/// </summary>
		public bool CanProduce(Recipe recipe) {
			if (recipe is null)
				return false;

			return candidateRecipes.Contains(recipe);
		}

		/// <summary>
		/// Probes the graph entry for the specified production recipe without treating it as an exact craftability verdict.
		/// </summary>
		/// <param name="recipe">The recipe to inspect.</param>
		public InventoryCraftabilityRecipeProbe ProbeRecipe(Recipe recipe) {
			if (recipe is null)
				return InventoryCraftabilityRecipeProbe.Rejected;

			return recipeProbesByRecipe.TryGetValue(recipe, out var probe)
				? probe
				: InventoryCraftabilityRecipeProbe.Rejected;
		}

		/// <summary>
		/// Attempts to get the graph node for a direct or virtually craftable item type.
		/// </summary>
		/// <param name="itemType">The item type to inspect.</param>
		/// <param name="node">The graph node, when the item type is reachable.</param>
		public bool TryGetNode(int itemType, out InventoryCraftabilityNode node) => nodes.TryGetValue(itemType, out node);

		/// <summary>
		/// Gets candidate recipes that can produce the specified item type within this graph.
		/// </summary>
		/// <param name="itemType">The item type to inspect.</param>
		public IReadOnlyList<InventoryCraftabilityCandidate> GetCandidates(int itemType)
			=> nodes.TryGetValue(itemType, out var node) ? node.Candidates : [];

		/// <summary>
		/// Attempts to get the recipe dependency strongly connected component for the specified recipe.
		/// </summary>
		/// <param name="recipe">The recipe to inspect.</param>
		/// <param name="component">The component containing the recipe.</param>
		public bool TryGetStronglyConnectedComponent(Recipe recipe, out RecipeStronglyConnectedComponent component) {
			if (recipeIngredientIndex is not null)
				return recipeIngredientIndex.TryGetStronglyConnectedComponent(recipe, out component);

			component = null;
			return false;
		}

		private static InventoryCraftabilityGraph CreateGraph(
			Dictionary<int, int> capacities,
			Dictionary<int, int> directCapacities,
			Dictionary<int, int> minimumDepths,
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType,
			HashSet<Recipe> candidateRecipeSet,
			RecipeIngredientIndex recipeIngredientIndex,
			bool canRejectMissingRecipes,
			int maxDepth
		) {
			Dictionary<int, InventoryCraftabilityNode> nodes = BuildNodes(capacities, directCapacities, minimumDepths, candidatesByItemType);

			return new InventoryCraftabilityGraph(
				capacities,
				nodes,
				candidateRecipeSet,
				BuildRecipeProbesByRecipe(nodes, candidatesByItemType, recipeIngredientIndex),
				recipeIngredientIndex,
				canRejectMissingRecipes,
				maxDepth);
		}

		private static bool TryGetPossibleOutput(Recipe recipe, Dictionary<int, int> capacities, out int outputQuantity, out int craftableBatches, out IReadOnlyList<InventoryCraftabilityDependency> dependencies) {
			long possibleBatches = recipe.requiredItem.Count <= 0 ? Item.CommonMaxStack : long.MaxValue;
			List<InventoryCraftabilityDependency> dependencyList = new(recipe.requiredItem.Count);

			foreach (Item ingredient in recipe.requiredItem) {
				int availableQuantity = GetIngredientCapacity(recipe, capacities, ingredient.type, out bool usesRecipeGroup, out int recipeGroupId);
				if (availableQuantity <= 0) {
					outputQuantity = 0;
					craftableBatches = 0;
					dependencies = [];
					return false;
				}

				long batches = availableQuantity / Math.Max(1, ingredient.stack);
				if (batches <= 0) {
					outputQuantity = 0;
					craftableBatches = 0;
					dependencies = [];
					return false;
				}

				if (batches < possibleBatches)
					possibleBatches = batches;

				dependencyList.Add(new InventoryCraftabilityDependency(ingredient.type, ingredient.stack, availableQuantity, usesRecipeGroup, recipeGroupId));
			}

			if (possibleBatches <= 0 || possibleBatches == long.MaxValue) {
				outputQuantity = 0;
				craftableBatches = 0;
				dependencies = [];
				return false;
			}

			long total = possibleBatches * Math.Max(1, recipe.createItem.stack);
			outputQuantity = total >= int.MaxValue ? int.MaxValue : (int)total;
			craftableBatches = possibleBatches >= int.MaxValue ? int.MaxValue : (int)possibleBatches;
			dependencies = dependencyList;
			return outputQuantity > 0;
		}

		private static RecipeIngredientIndex GetOrCreateIngredientIndex(IReadOnlyList<Recipe> recipes) {
			lock (ingredientIndexCacheLock) {
				if (object.ReferenceEquals(cachedIngredientIndexRecipes, recipes) && cachedIngredientIndex is not null)
					return cachedIngredientIndex;

				cachedIngredientIndexRecipes = recipes;
				cachedIngredientIndex = BuildRecipesByIngredientType(recipes);
				return cachedIngredientIndex;
			}
		}

		private static RecipeIngredientIndex BuildRecipesByIngredientType(IReadOnlyList<Recipe> recipes) {
			Dictionary<int, List<Recipe>> recipesByIngredientType = new();
			List<Recipe> recipesWithoutIngredients = [];

			foreach (Recipe recipe in recipes) {
				if (recipe is null || recipe.Disabled)
					continue;

				if (recipe.requiredItem.Count <= 0) {
					recipesWithoutIngredients.Add(recipe);
					continue;
				}

				foreach (Item ingredient in recipe.requiredItem)
					AddRecipeByIngredientType(recipesByIngredientType, ingredient.type, recipe);

				foreach (int groupID in recipe.acceptedGroups) {
					RecipeGroup group = RecipeGroup.recipeGroups[groupID];
					foreach (int groupItem in group.ValidItems)
						AddRecipeByIngredientType(recipesByIngredientType, groupItem, recipe);
				}
			}

			BuildRecipeStronglyConnectedComponents(recipes, recipesByIngredientType, out var componentsByRecipe, out var componentsInTopologicalOrder);
			return new RecipeIngredientIndex(recipesByIngredientType, recipesWithoutIngredients, componentsByRecipe, componentsInTopologicalOrder);
		}

		private static void BuildRecipeStronglyConnectedComponents(
			IReadOnlyList<Recipe> recipes,
			Dictionary<int, List<Recipe>> recipesByIngredientType,
			out Dictionary<Recipe, RecipeStronglyConnectedComponent> componentsByRecipe,
			out List<RecipeStronglyConnectedComponent> componentsInTopologicalOrder
		) {
			Dictionary<Recipe, List<Recipe>> edges = new(ReferenceEqualityComparer.Instance);
			Dictionary<Recipe, List<Recipe>> reverseEdges = new(ReferenceEqualityComparer.Instance);
			List<Recipe> enabledRecipes = [];

			foreach (Recipe recipe in recipes) {
				if (recipe is null || recipe.Disabled)
					continue;

				enabledRecipes.Add(recipe);
				edges[recipe] = [];
				reverseEdges[recipe] = [];
			}

			foreach (Recipe recipe in enabledRecipes) {
				if (!recipesByIngredientType.TryGetValue(recipe.createItem.type, out var consumers))
					continue;

				foreach (Recipe consumer in consumers) {
					if (consumer is null || consumer.Disabled || !edges.ContainsKey(consumer))
						continue;

					AddRecipeEdge(edges[recipe], consumer);
					AddRecipeEdge(reverseEdges[consumer], recipe);
				}
			}

			List<Recipe> finishOrder = BuildRecipeFinishOrder(enabledRecipes, edges);
			componentsByRecipe = new(ReferenceEqualityComparer.Instance);
			HashSet<Recipe> visited = new(ReferenceEqualityComparer.Instance);
			List<RecipeStronglyConnectedComponent> components = [];

			for (int i = finishOrder.Count - 1; i >= 0; i--) {
				Recipe root = finishOrder[i];
				if (!visited.Add(root))
					continue;

				List<Recipe> componentRecipes = [];
				Stack<Recipe> stack = new();
				stack.Push(root);

				while (stack.Count > 0) {
					Recipe recipe = stack.Pop();
					componentRecipes.Add(recipe);

					foreach (Recipe next in reverseEdges[recipe]) {
						if (visited.Add(next))
							stack.Push(next);
					}
				}

				bool isCyclic = componentRecipes.Count > 1 || HasSelfEdge(componentRecipes[0], edges);
				var component = new RecipeStronglyConnectedComponent(components.Count, componentRecipes, isCyclic);
				components.Add(component);
				foreach (Recipe recipe in componentRecipes)
					componentsByRecipe[recipe] = component;
			}

			AssignStronglyConnectedComponentOrder(components, componentsByRecipe, edges);

			componentsInTopologicalOrder = [.. components];
			componentsInTopologicalOrder.Sort((left, right) => left.TopologicalOrder.CompareTo(right.TopologicalOrder));
		}

		private static void AssignStronglyConnectedComponentOrder(
			List<RecipeStronglyConnectedComponent> components,
			Dictionary<Recipe, RecipeStronglyConnectedComponent> componentsByRecipe,
			Dictionary<Recipe, List<Recipe>> edges
		) {
			if (components.Count <= 0)
				return;

			Dictionary<int, HashSet<int>> componentEdges = new();
			int[] indegrees = new int[components.Count];

			foreach (var (recipe, recipeEdges) in edges) {
				if (!componentsByRecipe.TryGetValue(recipe, out var component))
					continue;

				foreach (Recipe next in recipeEdges) {
					if (!componentsByRecipe.TryGetValue(next, out var nextComponent) || component.Id == nextComponent.Id)
						continue;

					if (!componentEdges.TryGetValue(component.Id, out var outgoing))
						componentEdges[component.Id] = outgoing = [];

					if (outgoing.Add(nextComponent.Id))
						indegrees[nextComponent.Id]++;
				}
			}

			Queue<int> queue = new();
			for (int i = 0; i < indegrees.Length; i++) {
				if (indegrees[i] == 0)
					queue.Enqueue(i);
			}

			int order = 0;
			while (queue.Count > 0) {
				int id = queue.Dequeue();
				components[id].TopologicalOrder = order++;

				if (!componentEdges.TryGetValue(id, out var outgoing))
					continue;

				foreach (int nextId in outgoing) {
					indegrees[nextId]--;
					if (indegrees[nextId] == 0)
						queue.Enqueue(nextId);
				}
			}

			for (int i = 0; i < components.Count; i++) {
				if (components[i].TopologicalOrder < 0)
					components[i].TopologicalOrder = order++;
			}
		}

		private static List<Recipe> BuildRecipeFinishOrder(List<Recipe> recipes, Dictionary<Recipe, List<Recipe>> edges) {
			List<Recipe> finishOrder = new(recipes.Count);
			HashSet<Recipe> visited = new(ReferenceEqualityComparer.Instance);

			foreach (Recipe root in recipes) {
				if (!visited.Add(root))
					continue;

				Stack<(Recipe Recipe, int NextIndex)> stack = new();
				stack.Push((root, 0));

				while (stack.Count > 0) {
					var (recipe, nextIndex) = stack.Pop();
					var recipeEdges = edges[recipe];

					if (nextIndex >= recipeEdges.Count) {
						finishOrder.Add(recipe);
						continue;
					}

					stack.Push((recipe, nextIndex + 1));

					Recipe next = recipeEdges[nextIndex];
					if (visited.Add(next))
						stack.Push((next, 0));
				}
			}

			return finishOrder;
		}

		private static void AddRecipeEdge(List<Recipe> recipes, Recipe recipe) {
			if (!ContainsRecipeByReference(recipes, recipe))
				recipes.Add(recipe);
		}

		private static bool HasSelfEdge(Recipe recipe, Dictionary<Recipe, List<Recipe>> edges) {
			foreach (Recipe next in edges[recipe]) {
				if (object.ReferenceEquals(next, recipe))
					return true;
			}

			return false;
		}

		private static void AddRecipeByIngredientType(Dictionary<int, List<Recipe>> recipesByIngredientType, int ingredientType, Recipe recipe) {
			if (!recipesByIngredientType.TryGetValue(ingredientType, out var recipes))
				recipesByIngredientType[ingredientType] = recipes = [];

			if (!ContainsRecipeByReference(recipes, recipe))
				recipes.Add(recipe);
		}

		private static bool ContainsRecipeByReference(List<Recipe> recipes, Recipe recipe) {
			foreach (Recipe existing in recipes) {
				if (object.ReferenceEquals(existing, recipe))
					return true;
			}

			return false;
		}

		private static List<Recipe> GetCandidateRecipes(
			HashSet<int> frontier,
			RecipeIngredientIndex recipeIngredientIndex,
			bool includeRecipesWithoutIngredients,
			IEnumerable<Recipe> seedRecipes
		) {
			List<Recipe> candidates = [];
			HashSet<Recipe> seen = new(ReferenceEqualityComparer.Instance);

			if (seedRecipes is not null) {
				foreach (Recipe recipe in seedRecipes) {
					AddCandidateRecipe(recipe, recipeIngredientIndex, candidates, seen);
				}
			}

			if (includeRecipesWithoutIngredients) {
				foreach (Recipe recipe in recipeIngredientIndex.RecipesWithoutIngredients) {
					AddCandidateRecipe(recipe, recipeIngredientIndex, candidates, seen);
				}
			}

			foreach (int itemType in frontier) {
				if (!recipeIngredientIndex.RecipesByIngredientType.TryGetValue(itemType, out var recipes))
					continue;

				foreach (Recipe recipe in recipes) {
					AddCandidateRecipe(recipe, recipeIngredientIndex, candidates, seen);
				}
			}

			SortCandidateRecipesByComponentOrder(candidates, recipeIngredientIndex);
			return candidates;
		}

		private static void AddCandidateRecipe(Recipe recipe, RecipeIngredientIndex recipeIngredientIndex, List<Recipe> candidates, HashSet<Recipe> seen) {
			if (recipe is null || recipe.Disabled || !seen.Add(recipe))
				return;

			candidates.Add(recipe);

			if (!recipeIngredientIndex.TryGetStronglyConnectedComponent(recipe, out var component) || !component.IsCyclic)
				return;

			foreach (Recipe componentRecipe in component.Recipes) {
				if (componentRecipe is not null && !componentRecipe.Disabled && seen.Add(componentRecipe))
					candidates.Add(componentRecipe);
			}
		}

		private static void SortCandidateRecipesByComponentOrder(List<Recipe> candidates, RecipeIngredientIndex recipeIngredientIndex) {
			if (candidates.Count <= 1)
				return;

			candidates.Sort((left, right) => {
				int compare = recipeIngredientIndex.GetScheduleOrder(left).CompareTo(recipeIngredientIndex.GetScheduleOrder(right));
				if (compare != 0)
					return compare;

				return left.RecipeIndex.CompareTo(right.RecipeIndex);
			});
		}

		private static int GetIngredientCapacity(Recipe recipe, Dictionary<int, int> capacities, int ingredientType, out bool usesRecipeGroup, out int recipeGroupId) {
			long quantity = 0;
			usesRecipeGroup = false;
			recipeGroupId = -1;

			foreach (int groupID in recipe.acceptedGroups) {
				RecipeGroup group = RecipeGroup.recipeGroups[groupID];
				if (!group.ContainsItem(ingredientType))
					continue;

				usesRecipeGroup = true;
				if (recipeGroupId < 0)
					recipeGroupId = groupID;

				foreach (int groupItem in group.ValidItems) {
					quantity += GetCapacity(capacities, groupItem);
					if (quantity >= int.MaxValue)
						return int.MaxValue;
				}
			}

			if (!usesRecipeGroup)
				quantity = GetCapacity(capacities, ingredientType);

			return quantity >= int.MaxValue ? int.MaxValue : (int)quantity;
		}

		private static Dictionary<int, InventoryCraftabilityNode> BuildNodes(
			Dictionary<int, int> capacities,
			Dictionary<int, int> directCapacities,
			Dictionary<int, int> minimumDepths,
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType
		) {
			Dictionary<int, InventoryCraftabilityNode> nodes = new(capacities.Count);

			foreach (var (type, quantity) in capacities) {
				if (quantity <= 0)
					continue;

				directCapacities.TryGetValue(type, out int directQuantity);
				minimumDepths.TryGetValue(type, out int minimumDepth);
				candidatesByItemType.TryGetValue(type, out var candidates);

				nodes[type] = new InventoryCraftabilityNode(type, directQuantity, quantity, minimumDepth, candidates ?? []);
			}

			return nodes;
		}

		private static Dictionary<Recipe, InventoryCraftabilityRecipeProbe> BuildRecipeProbesByRecipe(
			Dictionary<int, InventoryCraftabilityNode> nodes,
			Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType,
			RecipeIngredientIndex recipeIngredientIndex
		) {
			Dictionary<Recipe, InventoryCraftabilityCandidate> candidatesByRecipe = new(ReferenceEqualityComparer.Instance);

			foreach (var candidates in candidatesByItemType.Values) {
				foreach (var candidate in candidates) {
					if (candidate.Recipe is null)
						continue;

					if (candidatesByRecipe.TryGetValue(candidate.Recipe, out var existing)) {
						if (candidate.Depth < existing.Depth || candidate.OutputQuantity > existing.OutputQuantity)
							candidatesByRecipe[candidate.Recipe] = candidate;
					} else
						candidatesByRecipe[candidate.Recipe] = candidate;
				}
			}

			Dictionary<Recipe, InventoryCraftabilityRecipeProbe> probesByRecipe = new(candidatesByRecipe.Count, ReferenceEqualityComparer.Instance);
			foreach (var (recipe, candidate) in candidatesByRecipe) {
				candidatesByItemType.TryGetValue(recipe.createItem.type, out var sameResultCandidates);

				int alternateCandidateCount = 0;
				if (sameResultCandidates is not null) {
					for (int i = 0; i < sameResultCandidates.Count; i++) {
						if (!object.ReferenceEquals(sameResultCandidates[i].Recipe, recipe))
							alternateCandidateCount++;
					}
				}

				InventoryCraftabilityProbeFlags flags = alternateCandidateCount > 0
					? InventoryCraftabilityProbeFlags.AlternateSameResultRecipe
					: InventoryCraftabilityProbeFlags.None;

				if (recipeIngredientIndex.TryGetStronglyConnectedComponent(recipe, out var component) && component.IsCyclic)
					flags |= InventoryCraftabilityProbeFlags.CyclicDependencyRegion;

				foreach (var dependency in candidate.Dependencies) {
					if (dependency.UsesRecipeGroup)
						flags |= InventoryCraftabilityProbeFlags.RecipeGroupDependency;

					if (GetDirectDependencyQuantity(nodes, dependency) < dependency.AvailableQuantity) {
						flags |= InventoryCraftabilityProbeFlags.VirtualDependency;

						if (!dependency.UsesRecipeGroup)
							flags |= InventoryCraftabilityProbeFlags.NonRecipeGroupVirtualDependency;
					}
				}

				if ((flags & (InventoryCraftabilityProbeFlags.AlternateSameResultRecipe | InventoryCraftabilityProbeFlags.RecipeGroupDependency | InventoryCraftabilityProbeFlags.VirtualDependency | InventoryCraftabilityProbeFlags.CyclicDependencyRegion)) == 0)
					flags |= InventoryCraftabilityProbeFlags.DirectRecipeAuthority;

				probesByRecipe[recipe] = new InventoryCraftabilityRecipeProbe(candidate, alternateCandidateCount, flags);
			}

			return probesByRecipe;
		}

		private static int GetDirectDependencyQuantity(Dictionary<int, InventoryCraftabilityNode> nodes, InventoryCraftabilityDependency dependency) {
			if (!dependency.UsesRecipeGroup)
				return nodes.TryGetValue(dependency.IngredientType, out var dependencyNode) ? dependencyNode.DirectQuantity : 0;

			long quantity = 0;
			RecipeGroup group = RecipeGroup.recipeGroups[dependency.RecipeGroupId];
			foreach (int itemType in group.ValidItems) {
				if (nodes.TryGetValue(itemType, out var groupNode))
					quantity += groupNode.DirectQuantity;

				if (quantity >= int.MaxValue)
					return int.MaxValue;
			}

			return (int)quantity;
		}

		private static void AddCandidate(Dictionary<int, List<InventoryCraftabilityCandidate>> candidatesByItemType, int itemType, InventoryCraftabilityCandidate candidate) {
			if (!candidatesByItemType.TryGetValue(itemType, out var candidates)) {
				candidatesByItemType[itemType] = [candidate];
				return;
			}

			for (int i = 0; i < candidates.Count; i++) {
				var existing = candidates[i];
				if (!object.ReferenceEquals(existing.Recipe, candidate.Recipe))
					continue;

				if (candidate.Depth < existing.Depth || candidate.OutputQuantity > existing.OutputQuantity)
					candidates[i] = candidate;

				return;
			}

			candidates.Add(candidate);
		}

		private static int GetCapacity(Dictionary<int, int> capacities, int itemType)
			=> capacities.TryGetValue(itemType, out int quantity) ? quantity : 0;

		private static void AddCapacity(Dictionary<int, int> capacities, int itemType, int quantity) {
			if (quantity <= 0)
				return;

			if (!capacities.TryGetValue(itemType, out int existing)) {
				capacities[itemType] = quantity;
				return;
			}

			long total = (long)existing + quantity;
			capacities[itemType] = total >= int.MaxValue ? int.MaxValue : (int)total;
		}
	}

	/// <summary>
	/// Cached recipe lookup data used by inventory craftability graph propagation.
	/// </summary>
	public sealed class RecipeIngredientIndex {
		/// <summary>
		/// Gets recipes grouped by accepted ingredient item type.
		/// </summary>
		public IReadOnlyDictionary<int, List<Recipe>> RecipesByIngredientType { get; }

		/// <summary>
		/// Gets recipes that have no item ingredients.
		/// </summary>
		public IReadOnlyList<Recipe> RecipesWithoutIngredients { get; }

		internal IReadOnlyList<RecipeStronglyConnectedComponent> ComponentsInTopologicalOrder { get; }

		private readonly Dictionary<Recipe, RecipeStronglyConnectedComponent> stronglyConnectedComponentsByRecipe;

		internal RecipeIngredientIndex(
			Dictionary<int, List<Recipe>> recipesByIngredientType,
			List<Recipe> recipesWithoutIngredients,
			Dictionary<Recipe, RecipeStronglyConnectedComponent> stronglyConnectedComponentsByRecipe,
			List<RecipeStronglyConnectedComponent> componentsInTopologicalOrder
		) {
			RecipesByIngredientType = recipesByIngredientType;
			RecipesWithoutIngredients = recipesWithoutIngredients;
			this.stronglyConnectedComponentsByRecipe = stronglyConnectedComponentsByRecipe;
			ComponentsInTopologicalOrder = componentsInTopologicalOrder;
		}

		/// <summary>
		/// Attempts to get the recipe dependency strongly connected component for the specified recipe.
		/// </summary>
		/// <param name="recipe">The recipe to inspect.</param>
		/// <param name="component">The component containing the recipe.</param>
		public bool TryGetStronglyConnectedComponent(Recipe recipe, out RecipeStronglyConnectedComponent component) {
			if (recipe is not null)
				return stronglyConnectedComponentsByRecipe.TryGetValue(recipe, out component);

			component = null;
			return false;
		}

		internal int GetScheduleOrder(Recipe recipe) {
			if (TryGetStronglyConnectedComponent(recipe, out var component))
				return component.TopologicalOrder;

			return int.MaxValue;
		}
	}

	/// <summary>
	/// A strongly connected recipe dependency component.
	/// </summary>
	public sealed class RecipeStronglyConnectedComponent {
		/// <summary>
		/// Gets the stable component ID within the cached recipe index.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Gets recipes in this dependency component.
		/// </summary>
		public IReadOnlyList<Recipe> Recipes { get; }

		/// <summary>
		/// Gets whether this component contains a recipe dependency cycle.
		/// </summary>
		public bool IsCyclic { get; }

		internal int TopologicalOrder { get; set; } = -1;

		internal RecipeStronglyConnectedComponent(int id, IReadOnlyList<Recipe> recipes, bool isCyclic) {
			Id = id;
			Recipes = recipes;
			IsCyclic = isCyclic;
		}
	}

	/// <summary>
	/// Describes graph-side probe traits for a concrete recipe candidate.
	/// </summary>
	[Flags]
	public enum InventoryCraftabilityProbeFlags {
		/// <summary>
		/// No graph-side traits were recorded.
		/// </summary>
		None = 0,

		/// <summary>
		/// Another recipe candidate produced the same result item.
		/// </summary>
		AlternateSameResultRecipe = 1 << 0,

		/// <summary>
		/// At least one dependency used recipe-group capacity.
		/// </summary>
		RecipeGroupDependency = 1 << 1,

		/// <summary>
		/// At least one dependency used quantity that included virtual graph output.
		/// </summary>
		VirtualDependency = 1 << 2,

		/// <summary>
		/// This candidate is equivalent to a direct single-recipe availability result.
		/// </summary>
		DirectRecipeAuthority = 1 << 3,

		/// <summary>
		/// The recipe belongs to a cyclic dependency region.
		/// </summary>
		CyclicDependencyRegion = 1 << 4,

		/// <summary>
		/// At least one non-recipe-group dependency used quantity that included virtual graph output.
		/// </summary>
		NonRecipeGroupVirtualDependency = 1 << 5
	}

	/// <summary>
	/// A safe graph-side probe for one concrete production recipe.
	/// </summary>
	public readonly struct InventoryCraftabilityRecipeProbe {
		/// <summary>
		/// Gets a rejected probe result.
		/// </summary>
		public static InventoryCraftabilityRecipeProbe Rejected { get; } = new();

		/// <summary>
		/// Gets whether the graph observed this concrete recipe as a candidate.
		/// </summary>
		public bool HasCandidate { get; }

		/// <summary>
		/// Gets the graph candidate for this recipe, when observed.
		/// </summary>
		public InventoryCraftabilityCandidate Candidate { get; }

		/// <summary>
		/// Gets the number of alternate candidate recipes that produced the same result item in the same graph.
		/// </summary>
		public int AlternateCandidateCount { get; }

		/// <summary>
		/// Gets graph-side traits and ambiguity reasons for this concrete recipe candidate.
		/// </summary>
		public InventoryCraftabilityProbeFlags Flags { get; }

		/// <summary>
		/// Gets whether this candidate used recipe-group capacity for any dependency.
		/// </summary>
		public bool UsesRecipeGroups => (Flags & InventoryCraftabilityProbeFlags.RecipeGroupDependency) != 0;

		/// <summary>
		/// Gets whether this candidate consumed a dependency quantity that included virtual graph output.
		/// </summary>
		public bool UsesVirtualDependencies => (Flags & InventoryCraftabilityProbeFlags.VirtualDependency) != 0;

		/// <summary>
		/// Gets whether the candidate has graph-side ambiguity that requires exact demand planning before it can be trusted positively.
		/// </summary>
		public bool IsAmbiguous => (Flags & (InventoryCraftabilityProbeFlags.AlternateSameResultRecipe | InventoryCraftabilityProbeFlags.RecipeGroupDependency | InventoryCraftabilityProbeFlags.VirtualDependency | InventoryCraftabilityProbeFlags.CyclicDependencyRegion)) != 0;

		/// <summary>
		/// Gets whether this graph candidate is equivalent to a direct single-recipe availability result for the concrete recipe.
		/// </summary>
		public bool IsDirectRecipeAuthority => (Flags & InventoryCraftabilityProbeFlags.DirectRecipeAuthority) != 0;

		internal InventoryCraftabilityRecipeProbe(
			InventoryCraftabilityCandidate candidate,
			int alternateCandidateCount,
			InventoryCraftabilityProbeFlags flags
		) {
			HasCandidate = true;
			Candidate = candidate;
			AlternateCandidateCount = alternateCandidateCount;
			Flags = flags;
		}
	}

	/// <summary>
	/// A direct or virtual item node in an <see cref="InventoryCraftabilityGraph" />.
	/// </summary>
	public sealed class InventoryCraftabilityNode {
		/// <summary>
		/// Gets the item type represented by this node.
		/// </summary>
		public int ItemType { get; }

		/// <summary>
		/// Gets the quantity available directly from the inventory snapshot.
		/// </summary>
		public int DirectQuantity { get; }

		/// <summary>
		/// Gets the direct plus virtually craftable quantity observed by the graph.
		/// </summary>
		public int TotalQuantity { get; }

		/// <summary>
		/// Gets the shallowest graph layer that reached this item type.
		/// </summary>
		public int MinimumDepth { get; }

		/// <summary>
		/// Gets the candidate recipes that can produce this item type.
		/// </summary>
		public IReadOnlyList<InventoryCraftabilityCandidate> Candidates { get; }

		internal InventoryCraftabilityNode(int itemType, int directQuantity, int totalQuantity, int minimumDepth, IReadOnlyList<InventoryCraftabilityCandidate> candidates) {
			ItemType = itemType;
			DirectQuantity = directQuantity;
			TotalQuantity = totalQuantity;
			MinimumDepth = minimumDepth;
			Candidates = candidates;
		}
	}

	/// <summary>
	/// A recipe candidate that can produce a graph node within a bounded depth.
	/// </summary>
	public sealed class InventoryCraftabilityCandidate {
		/// <summary>
		/// Gets the recipe represented by this candidate.
		/// </summary>
		public Recipe Recipe { get; }

		/// <summary>
		/// Gets the graph layer where this candidate was observed.
		/// </summary>
		public int Depth { get; }

		/// <summary>
		/// Gets the estimated craftable recipe batch count at this graph layer.
		/// </summary>
		public int CraftableBatches { get; }

		/// <summary>
		/// Gets the estimated result-item quantity at this graph layer.
		/// </summary>
		public int OutputQuantity { get; }

		/// <summary>
		/// Gets the dependency capacities used when this candidate was observed.
		/// </summary>
		public IReadOnlyList<InventoryCraftabilityDependency> Dependencies { get; }

		internal InventoryCraftabilityCandidate(Recipe recipe, int depth, int craftableBatches, int outputQuantity, IReadOnlyList<InventoryCraftabilityDependency> dependencies) {
			Recipe = recipe;
			Depth = depth;
			CraftableBatches = craftableBatches;
			OutputQuantity = outputQuantity;
			Dependencies = dependencies;
		}
	}

	/// <summary>
	/// A dependency capacity snapshot for an <see cref="InventoryCraftabilityCandidate" />.
	/// </summary>
	public readonly struct InventoryCraftabilityDependency {
		/// <summary>
		/// Gets the recipe ingredient item type.
		/// </summary>
		public int IngredientType { get; }

		/// <summary>
		/// Gets the quantity required for one recipe batch.
		/// </summary>
		public int RequiredQuantityPerBatch { get; }

		/// <summary>
		/// Gets the direct or virtual quantity available to this dependency at the candidate layer.
		/// </summary>
		public int AvailableQuantity { get; }

		/// <summary>
		/// Gets whether the dependency can be satisfied by a recipe group.
		/// </summary>
		public bool UsesRecipeGroup { get; }

		/// <summary>
		/// Gets the recipe group ID when <see cref="UsesRecipeGroup" /> is <see langword="true" />.
		/// </summary>
		public int RecipeGroupId { get; }

		internal InventoryCraftabilityDependency(int ingredientType, int requiredQuantityPerBatch, int availableQuantity, bool usesRecipeGroup, int recipeGroupId) {
			IngredientType = ingredientType;
			RequiredQuantityPerBatch = requiredQuantityPerBatch;
			AvailableQuantity = availableQuantity;
			UsesRecipeGroup = usesRecipeGroup;
			RecipeGroupId = recipeGroupId;
		}
	}
}
