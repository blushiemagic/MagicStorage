using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public sealed class OrderedRecipeTree {
		private readonly List<OrderedRecipeTree> leaves = new();
		public readonly OrderedRecipeContext context;
		public readonly int parentLeafIndex;

		public IReadOnlyList<OrderedRecipeTree> Leaves => leaves;

		public OrderedRecipeTree Root { get; private set; }

		[MemberNotNullWhen(true, nameof(context))]
		public bool Invalid => context is null;

		public OrderedRecipeTree(OrderedRecipeContext context, int parentLeafIndex) {
			this.context = context;
			this.parentLeafIndex = parentLeafIndex;
			context?.LinkTo(this);
		}

		public void Add(OrderedRecipeTree tree) {
			leaves.Add(tree);
			tree.Root = this;
		}

		public void AddRange(IEnumerable<OrderedRecipeTree> trees) {
			foreach (var tree in trees) {
				leaves.Add(tree);
				tree.Root = this;
			}
		}

		public void Clear() {
			if (!Invalid)
				context.amountToCraft.Reset();
			leaves.Clear();
		}

		public Stack<OrderedRecipeContext> GetProcessingOrder(CancellationToken cancellationToken = default) {
			if (Invalid)
				return new();

			Stack<OrderedRecipeContext> recipeStack = new();
			Queue<OrderedRecipeTree> treeQueue = new();
			treeQueue.Enqueue(this);

			while (treeQueue.TryDequeue(out OrderedRecipeTree branch)) {
				cancellationToken.ThrowIfCancellationRequested();

				if (branch.Invalid)
					continue;  // Invalid branch

				recipeStack.Push(branch.context);

				foreach (var leaf in branch.Leaves)
					treeQueue.Enqueue(leaf);
			}

			return recipeStack;
		}

		/// <summary>
		/// Trims the branches of any trees whose ingredient requirement is met or does not have its crafting station and recipe condition requirements met.<br/>
		/// Use <see cref="GetProcessingOrder"/> to get the updated order of the recipe contexts.
		/// </summary>
		/// <param name="available"></param>
		/// <param name="cancellationToken">The cancellation token for refresh-thread callers.</param>
		public void TrimBranches(AvailableRecipeObjects available, CancellationToken cancellationToken = default) {
			TrimBranches(available, cancellationToken, trace: null);
		}

		internal void TrimBranches(AvailableRecipeObjects available, CancellationToken cancellationToken, ExactCraftingTrace trace) {
			/*
			if (!CraftingGUI.disableNetPrintingForIsAvailable)
				NetHelper.Report(true, "Trimming branches of recipe tree...");
			*/

			if (Invalid)
				return;

			if (available.creativeUnitPresent) {
				/*
				if (!CraftingGUI.disableNetPrintingForIsAvailable)
					NetHelper.Report(false, "Creative unit is present, forcing root to be trimmed.");
				*/

				Clear();
				return;
			}

			// Go from the top of the tree down, cutting off any branches when necessary
			Queue<OrderedRecipeTree> queue = new();
			foreach (var leaf in leaves)
				queue.Enqueue(leaf);

			while (queue.TryDequeue(out OrderedRecipeTree branch)) {
				cancellationToken.ThrowIfCancellationRequested();

				if (branch.Invalid)
					continue;  // Invalid branch, ignore

				Recipe recipe = branch.context.recipe;
				int result = recipe.createItem.type;

				bool trimBranch;
				int count = 0;

				if (available.creativeUnitPresent) {
					// All ingredient requirements are always met
					trimBranch = true;
					goto SkipIngredientChecks;
				}

				// Check if the amount needed has been satisfied
				// If it is, this recipe and its children are not needed
				Recipe parentRecipe = branch.Root.context.recipe;

				count = available.GetTotalIngredientQuantity(parentRecipe, result);
				Item ingredient = parentRecipe.requiredItem[branch.parentLeafIndex];
				int requiredAmount = branch.context.amountToCraft;
				trimBranch = available.isItemInfinite.Contains(ingredient.type) || count >= requiredAmount || !available.CanUseRecipe(recipe);
				trace?.Add(branch.context.depth, $"trim-check {DemandPlanningTrace.DescribeRecipe(recipe)} parent={DemandPlanningTrace.DescribeRecipe(parentRecipe)} ingredient={DemandPlanningTrace.DescribeItem(result)} count={count} required={requiredAmount} canUse={available.CanUseRecipe(recipe)} trim={trimBranch}");

				SkipIngredientChecks:

				if (trimBranch) {
					/*
					if (!CraftingGUI.disableNetPrintingForIsAvailable)
						NetHelper.Report(false, $"Branch trimmed: Depth = {branch.context.depth}, Recipe result = {recipe.createItem.stack} {Lang.GetItemNameValue(result)}");
					*/

					branch.Clear();
					trace?.Add(branch.context.depth, $"trimmed exact branch {DemandPlanningTrace.DescribeRecipe(recipe)}");
				} else {
					SharedCounter remaining = branch.context.amountToCraft;

					remaining -= count;

					remaining.EnsureNotNegative();
					trace?.Add(branch.context.depth, $"keep exact branch {DemandPlanningTrace.DescribeRecipe(recipe)} remaining={remaining}");

					// Check the leaves
					foreach (var leaf in branch.Leaves)
						queue.Enqueue(leaf);
				}
			}
		}

		/// <summary>
		/// Returns an enumeration of every recipe used in this recursion tree
		/// </summary>
		public IEnumerable<Recipe> GetAllRecipes(CancellationToken cancellationToken = default) {
			if (Invalid)
				yield break;

			Queue<OrderedRecipeTree> treeQueue = new();
			HashSet<Recipe> usedRecipes = new HashSet<Recipe>(ReferenceEqualityComparer.Instance);
			treeQueue.Enqueue(this);

			while (treeQueue.TryDequeue(out OrderedRecipeTree branch)) {
				cancellationToken.ThrowIfCancellationRequested();

				if (branch.Invalid || branch.context.amountToCraft <= 0)
					continue;

				Recipe recipe = branch.context.recipe;

				if (usedRecipes.Add(recipe))
					yield return recipe;

				foreach (var leaf in branch.Leaves)
					treeQueue.Enqueue(leaf);
			}
		}

		public IEnumerable<int> GetRequiredTiles() {
			return GetAllRecipes().SelectMany(static r => r.requiredTile).Distinct();
		}

		public bool HasCondition(Condition condition) {
			return GetAllRecipes().Any(r => r.HasCondition(condition));
		}

		public void GetCraftingInformation(AvailableRecipeObjects available, out CraftResult result, CancellationToken cancellationToken = default) {
			GetCraftingInformation(available, out result, cancellationToken, trace: null);
		}

		internal void GetCraftingInformation(AvailableRecipeObjects available, out CraftResult result, CancellationToken cancellationToken, ExactCraftingTrace trace) {
			if (Invalid) {
				trace?.Add(0, "exact result unavailable: invalid tree");
				result = default;
				return;
			}

			// Get the info for one craft, then multiply the contents by how many batches would be needed
			var recipeStack = GetProcessingOrder(cancellationToken);
			trace?.Add(0, $"exact processing order count={recipeStack.Count}");

			// Check each context in the stack and bail immediately if any were invalid
			foreach (OrderedRecipeContext context in recipeStack) {
				cancellationToken.ThrowIfCancellationRequested();

				if (context is null) {
					trace?.Add(0, "exact result unavailable: null context in processing order");
					result = default;
					return;
				}
			}

			Dictionary<int, int> itemIndices = new();
			Dictionary<int, int> groupIndices = new();
			Dictionary<int, int> excessIndicies = new();

			result = CraftResult.Default;
			var recipes = result.usedRecipes;
			var materials = result.requiredMaterials;
			var excessResults = result.excessResults;
			var requiredTiles = result.requiredTiles;
			var requiredConditions = result.requiredConditions;

			EnvironmentSandbox sandbox;
			IEnumerable<EnvironmentModule> modules;
			if (CraftingGUI.GetHeart() is { } heart) {
				sandbox = new EnvironmentSandbox(Main.LocalPlayer, heart);
				modules = heart.GetModules();
			} else {
				sandbox = default;
				modules = [];
			}

			// NOTE: [ThreadStatic] only runs the field initializer on one thread
			CraftingGUI.DroppedItems ??= new();

			foreach (OrderedRecipeContext context in recipeStack) {
				cancellationToken.ThrowIfCancellationRequested();

				// Trimmed branch?  Ignore
				if (context.amountToCraft <= 0)
					continue;

				int ingredientBatches = (int)Math.Ceiling(context.amountToCraft / (double)context.recipe.createItem.stack);
				
				Recipe recipe = context.recipe;
				trace?.Add(context.depth, $"process exact context {DemandPlanningTrace.DescribeRecipe(recipe)} amount={context.amountToCraft} batches={ingredientBatches}");

				if (available is not null && available.creativeUnitPresent)
					goto SkipIngredientChecks;

				int ingredientIndex = 0;
				foreach (Item item in recipe.requiredItem) {
					cancellationToken.ThrowIfCancellationRequested();

					// Consume from the excess results first
					SharedCounter stack = context.RentIngredientCounter(ingredientIndex, item.stack * ingredientBatches);
					ingredientIndex++;

					if (available is not null && available.isItemInfinite.Contains(item.type)) {
						// The ingredient doesn't need to be crafted, so skip it
						stack.Reset();
						continue;
					}

					if (excessIndicies.TryGetValue(item.type, out int excessIndex)) {
						ExcessItemInfo info = excessResults[excessIndex];

						if (info.Stack >= stack) {
							info.UpdateStack(-stack);
							continue;
						} else {
							stack -= info.Stack;
							info.ClearStack();
						}
					}

					bool usedRecipeGroup = false;
					foreach (int groupID in recipe.acceptedGroups) {
						RecipeGroup group = RecipeGroup.recipeGroups[groupID];
						if (group.ContainsItem(item.type)) {
							// Consume from the excess results first
							foreach (int groupItem in group.ValidItems) {
								if (excessIndicies.TryGetValue(groupItem, out excessIndex)) {
									ExcessItemInfo info = excessResults[excessIndex];

									if (info.Stack >= stack) {
										info.UpdateStack(-stack);
										stack.Reset();
										continue;
									} else {
										stack -= info.Stack;
										info.ClearStack();
									}

									usedRecipeGroup = true;
									
									if (stack <= 0)
										goto checkNonGroup;
								}
							}

							usedRecipeGroup = true;
							
							// Check if the recipe group has already been used
							if (!groupIndices.TryGetValue(groupID, out int index)) {
								groupIndices[groupID] = materials.Count;
								materials.Add(RequiredMaterialInfo.FromGroup(group, stack));
							} else
								materials[index].UpdateStack(stack);

							break;
						}
					}

					checkNonGroup:

					if (!usedRecipeGroup) {
						if (!itemIndices.TryGetValue(item.type, out int index)) {
							itemIndices[item.type] = materials.Count;
							materials.Add(RequiredMaterialInfo.FromItem(item.type, stack));
						} else
							materials[index].UpdateStack(stack);
					}
				}

				SkipIngredientChecks:

				// Fake a craft
				Item createItem = recipe.createItem.Clone();
				createItem.Prefix(-1);

				List<Item> droppedItems = new();

				for (int i = 0; i < ingredientBatches; i++) {
					droppedItems.AddRange(ExtraCraftItemsSystem.GetSimulatedItemDrops(recipe));

					foreach (EnvironmentModule module in modules)
						module.OnConsumeItemsForRecipe(sandbox, recipe, recipe.requiredItem);
				}

				// Add the result item and any dropped items to the excess list
				createItem.stack *= ingredientBatches;

				if (!excessIndicies.TryGetValue(createItem.type, out int itemIndex)) {
					excessIndicies[createItem.type] = excessResults.Count;
					excessResults.Add(new ExcessItemInfo(createItem.type, context.RentCounterFromParent(createItem.stack), createItem.prefix));
				} else
					excessResults[itemIndex].UpdateStack(createItem.stack);

				foreach (Item item in droppedItems) {
					if (!excessIndicies.TryGetValue(item.type, out itemIndex)) {
						excessIndicies[item.type] = excessResults.Count;
						excessResults.Add(new ExcessItemInfo(item.type, new SharedCounter(item.stack), item.prefix));
					} else
						excessResults[itemIndex].UpdateStack(item.stack);
				}

				recipes.Add(new RecursedRecipe(context.depth, recipe));
				requiredTiles.UnionWith(recipe.requiredTile);
				requiredConditions.UnionWith(recipe.Conditions);
			}

			trace?.Add(0, $"exact result materials={materials.Count} excess={excessResults.Count} recipes={recipes.Count}");
		}
	}
}
