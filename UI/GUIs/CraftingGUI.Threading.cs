using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.CrossMod;
using MagicStorage.UI.States;
using System.Collections.Generic;
using System.Text;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static StorageViewControls CreateRefreshThreadControls(BaseStorageUI refreshingUI) {
			var craftingPage = refreshingUI.GetDefaultPage<CraftingUIState.RecipesPage>();

			return new StorageViewControls(
				sortingOption: SortingOptionLoader.Selected,
				filteringOption: FilteringOptionLoader.Selected,
				generalFilters: FilteringOptionLoader.GeneralSelections,
				fullSearchText: craftingPage.searchBar.State.InputText,
				showOnlyFavorites: MagicStorageConfig.CraftingFavoritingEnabled && craftingPage.recipeButtons.Choice == RecipeButtonsFavoritesChoice,
				modSearchOption: craftingPage.modSearchBox.ModIndex
			);
		}

		public class CraftingRefreshThread : RefreshThread, IStorageItemsPovider, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider, IRecipeItemsProvider, ICraftingRefreshTimingProvider {
			public override bool IsPartialThread => false;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => CraftingGUI.FullRefreshBuilder.Instance;

			public StorageItems StorageItems { get; } = new();

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public MainZoneObjectsFilterControls<Recipe> MainZoneObjectsFilterControls { get; }

			public MainZoneObjectResults<Recipe> MainZoneObjectsResults { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<Recipe> CraftingObject { get; }

			public CraftObjectAvailableCache<Recipe> CraftObjectAvailableCache { get; }

			public RecipeSimulations RecipeSimulations { get; }

			public RecipeSnapshots RecipeSnapshots { get; } = new();

			public RecipeItems RecipeItems { get; }

			public CraftingRefreshTiming RefreshTiming { get; } = new();

			public CraftingRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<Recipe> mainZoneControls,
				MainZoneObjectResults<Recipe> mainZoneResults,
				IngredientControls ingredientControls,
				CraftingObject<Recipe> craftingObject,
				CraftObjectAvailableCache<Recipe> availableCache,
				RecipeSimulations simulations,
				RecipeItems recipeItems
			) : base(MagicUI.craftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				MainZoneObjectsResults = mainZoneResults;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				CraftObjectAvailableCache = availableCache;
				RecipeSimulations = simulations;
				RecipeItems = recipeItems;
				RecipeItems.itemResolver = ProcessedStorageItems.CreateItemResolver();
			}

			protected override void CollectObjects() {
				CraftingGUI.hasCompleteData = false;
				CraftingGUI.ResetRecentRecipeCache();

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PreRefreshRecipes(sandbox);

				StorageItems.CollectObjects(this);
				ProcessedStorageItems.CollectObjects(this);
				IngredientControls.CollectObjects(this);
				MainZoneObjectsResults.CollectObjects();

				AnalyzeIngredients();
				
				MainZoneObjectsFilterControls.SetAdjTiles([.. CraftingGUI.adjTiles]);
				MainZoneObjectsFilterControls.filterProvider = new StandardRecipeFilterProvider(this);

				RecipeSnapshots.CollectObjects();

				foreach (EnvironmentModule module in base.Heart.GetModules())
					module.ResetPlayer(sandbox);
			}

			protected override void Execute() {
				CraftingGUI.SortAndFilter(this);

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PostRefreshRecipes(sandbox);

				InitTaskSchedule(7, "Updating Caches");

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				CompleteOne();
				MainZoneObjectsResults.CopyToStaticCollections();
				CompleteOne();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CompleteOne();
				CraftObjectAvailableCache.CopyToStaticCollection();
				CompleteOne();
				RecipeSimulations.CopyToStaticCollection();
				CompleteOne();
				RecipeItems.CopyToStaticCollections();
				CompleteOne();

				CraftingGUI.SetRecipeAndCraftingCaches(this);
				CompleteOne();

				StorageGUI.hasAnyErrorItems = base.foundErrorItem;
				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
				CraftingGUI.lastKnownRecursionErrorForStoredItems = base.storedItemsError;

				CraftingGUI.hasCompleteData = true;
				RefreshTiming.Report("full");
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				MainZoneObjectsResults.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
				CraftObjectAvailableCache.ClearStaticCollection();
				RecipeSimulations.ClearStaticCollection();
				RecipeItems.ClearStaticCollections();
				StorageGUI.hasAnyErrorItems = false;
			}

			// Unused due to being a full thread
			public override void PrepareUIZones() { }

			public override void PopulateUIZones() { }
		}

		public class RecipeListRefreshThread : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider, IRecipeItemsProvider, ICraftingRefreshTimingProvider {
			public bool[] recipeConditionsMetSnapshot;
			private readonly bool _refreshInventorySnapshot;
			private readonly bool _refreshSelectedRecipeSnapshot;
			private readonly HashSet<int> _focusedGraphFrontierItemTypes;

			public override bool IsPartialThread => true;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => CraftingGUI.FullRefreshBuilder.Instance;

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public MainZoneObjectsFilterControls<Recipe> MainZoneObjectsFilterControls { get; }

			public MainZoneObjectResults<Recipe> MainZoneObjectsResults { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<Recipe> CraftingObject { get; }

			public CraftObjectAvailableCache<Recipe> CraftObjectAvailableCache { get; }

			public RecipeSimulations RecipeSimulations { get; }

			public RecipeSnapshots RecipeSnapshots { get; } = new();

			public RecipeItems RecipeItems { get; }

			public CraftingRefreshTiming RefreshTiming { get; } = new();

			public RecipeListRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<Recipe> mainZoneControls,
				MainZoneObjectResults<Recipe> mainZoneResults,
				IngredientControls ingredientControls,
				CraftingObject<Recipe> craftingObject,
				CraftObjectAvailableCache<Recipe> availableCache,
				RecipeSimulations simulations,
				RecipeItems recipeItems,
				bool refreshInventorySnapshot,
				bool refreshSelectedRecipeSnapshot,
				HashSet<int> focusedGraphFrontierItemTypes
			) : base(MagicUI.craftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				MainZoneObjectsResults = mainZoneResults;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				CraftObjectAvailableCache = availableCache;
				RecipeSimulations = simulations;
				RecipeItems = recipeItems;
				RecipeItems.itemResolver = ProcessedStorageItems.CreateItemResolver();
				_refreshInventorySnapshot = refreshInventorySnapshot;
				_refreshSelectedRecipeSnapshot = refreshSelectedRecipeSnapshot;
				_focusedGraphFrontierItemTypes = focusedGraphFrontierItemTypes;
			}

			protected override void CollectObjects() {
				cancellationToken.ThrowIfCancellationRequested();

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PreRefreshRecipes(sandbox);

				if (_refreshInventorySnapshot) {
					StorageItems storageItems = new();
					storageItems.CollectObjects(this);
					ProcessedStorageItems.CollectObjects(this);
					IngredientControls.CollectObjects(this);

					if (_refreshSelectedRecipeSnapshot)
						CraftingGUI.LoadItemsAndSetDictionaryInfo(this, storageItems);
					else {
						ProcessedStorageItems.CopyDisplayCollectionsFromStatic();
						CraftingGUI.LoadInventoryCountsOnly(this, storageItems);
					}
				} else {
					ProcessedStorageItems.CopyFromStaticCollectionsAndFields();
					IngredientControls.CopyFromStaticCollectionsAndFields();
				}

				MainZoneObjectsResults.CollectObjects();

				AnalyzeIngredients();
				
				MainZoneObjectsFilterControls.SetAdjTiles([.. CraftingGUI.adjTiles]);
				MainZoneObjectsFilterControls.filterProvider = new StandardRecipeFilterProvider(this);

				RecipeSnapshots.CollectObjects();
				PrepareInventoryCraftabilityGraph(
					this,
					ShouldBuildInventoryCraftabilityGraphForPartial(this, _refreshInventorySnapshot),
					_focusedGraphFrontierItemTypes,
					MainZoneObjectsResults.objectsToRefresh);

				foreach (EnvironmentModule module in base.Heart.GetModules())
					module.ResetPlayer(sandbox);
			}

			protected override void Execute() {
				CraftingGUI.RefreshRecipes(this);

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PostRefreshRecipes(sandbox);

				InitTaskSchedule(_refreshSelectedRecipeSnapshot ? 7 : 5, "Updating Caches");

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				CompleteOne();
				MainZoneObjectsResults.CopyToStaticCollections();
				CompleteOne();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CompleteOne();
				CraftObjectAvailableCache.CopyToStaticCollection();
				CompleteOne();
				RecipeSimulations.CopyToStaticCollection();
				CompleteOne();

				if (_refreshSelectedRecipeSnapshot) {
					CraftingGUI.RefreshStorageItems(this);
					RecipeItems.CopyToStaticCollections();
					CompleteOne();

					CraftingGUI.SetRecipeAndCraftingCaches(this);
					CompleteOne();

					CraftingGUI.lastKnownRecursionErrorForStoredItems = base.storedItemsError;
				}

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;

				CraftingGUI.hasCompleteData = true;
				RefreshTiming.Report("recipe-list");
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				// Partial recipe-list refreshes are double-buffered: keep the last
				// complete UI snapshot visible until this thread successfully
				// publishes its recalculated providers in Execute().
			}

			public override void PrepareUIZones() { }

			public override void PopulateUIZones() {
				refreshingUI.GetDefaultPage().Refresh();

				if (_refreshSelectedRecipeSnapshot && refreshingUI is CraftingUIState craftingUI)
					craftingUI.RefreshRecipePanel();
			}
		}

		public class RecipeInfoPanelRefreshThread : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSimulationsProvider, IRecipeSnapshotsProvider, IRecipeItemsProvider, ICraftingRefreshTimingProvider {
			public override bool IsPartialThread => true;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => CraftingGUI.FullRefreshBuilder.Instance;

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public MainZoneObjectsFilterControls<Recipe> MainZoneObjectsFilterControls { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<Recipe> CraftingObject { get; }

			public CraftObjectAvailableCache<Recipe> CraftObjectAvailableCache { get; }

			public RecipeSimulations RecipeSimulations { get; }

			public RecipeSnapshots RecipeSnapshots { get; } = new();

			public RecipeItems RecipeItems { get; }

			public CraftingRefreshTiming RefreshTiming { get; } = new();

			internal bool HasRestoredSelectedPreviewCaches { get; private set; }
			internal CraftingSimulationContext RestoredSelectedPreviewContext { get; private set; }
			internal int RestoredSelectedPreviewAmountCraftable { get; private set; }
			internal bool RestoredSelectedPreviewIsAvailable { get; private set; }
			internal bool RestoredSelectedPreviewPassesBlock { get; private set; }

			public RecipeInfoPanelRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<Recipe> mainZoneControls,
				IngredientControls ingredientControls,
				CraftingObject<Recipe> craftingObject,
				CraftObjectAvailableCache<Recipe> availableCache,
				RecipeSimulations simulations,
				RecipeItems recipeItems
			) : base(MagicUI.craftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				IngredientControls = ingredientControls;
				CraftObjectAvailableCache = availableCache;
				RecipeSimulations = simulations;
				CraftingObject = craftingObject;
				RecipeItems = recipeItems;
				RecipeItems.itemResolver = ProcessedStorageItems.CreateItemResolver();
			}

			protected override void CollectObjects() {
				cancellationToken.ThrowIfCancellationRequested();

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PreRefreshRecipes(sandbox);

				ProcessedStorageItems.CopyFromStaticCollectionsAndFields();
				IngredientControls.CopyFromStaticCollectionsAndFields();
				CraftObjectAvailableCache.CopyFromStaticCollection();
				RecipeSimulations.CopyFromStaticCollection();

				AnalyzeIngredients();
				
				MainZoneObjectsFilterControls.SetAdjTiles([.. CraftingGUI.adjTiles]);
				MainZoneObjectsFilterControls.filterProvider = new StandardRecipeFilterProvider(this);

				Recipe recipe = CraftingObject.selection.Value;
				if (!ShouldCollectFullRecipeSnapshots(recipe))
					RecipeSnapshots.CollectSingleObject(recipe);

				AttachCachedInventoryCraftabilityGraphIfAvailable(this);

				foreach (EnvironmentModule module in base.Heart.GetModules())
					module.ResetPlayer(sandbox);
			}

			protected override void Execute() {
				cancellationToken.ThrowIfCancellationRequested();

				var recipe = CraftingObject.selection.Value;
				int recipeAmountTarget = CraftingObject.craftAmountTarget.Value;

				if (ShouldCollectFullRecipeSnapshots(recipe))
					RecipeSnapshots.CollectObjects(cancellationToken);

				AttachCachedInventoryCraftabilityGraphIfAvailable(this);

				if (recipe is not null) {
					CraftObjectAvailableCache.lookup.Remove(recipe);
					RecipeSimulations.recipeToAvailableSimulation.Remove(recipe);
				}

				bool restoredPreview = CraftingGUI.TryRestoreSelectedRecipePreviewCache(this, recipe);
				if (!restoredPreview)
					CraftingGUI.RefreshStorageItems(this);

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PostRefreshRecipes(sandbox);

				InitTaskSchedule(6, "Updating Caches");

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				CompleteOne();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CompleteOne();
				RecipeItems.CopyToStaticCollections();
				CompleteOne();

				CraftingGUI.SetRecipeAndCraftingCaches(this);
				CompleteOne();

				if (!restoredPreview && CraftingObject.craftAmountTarget.Value == recipeAmountTarget)
					CraftingGUI.StoreSelectedRecipePreviewCache(this, recipe);

				CraftObjectAvailableCache.CopyToStaticCollection();
				CompleteOne();
				RecipeSimulations.CopyToStaticCollection();
				CompleteOne();
				
				CraftingGUI.lastKnownRecursionErrorForStoredItems = base.storedItemsError;

				CraftingGUI.hasCompleteData = true;
				RefreshTiming.Report("selected-preview");
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				// Partial recipe-panel refreshes are double-buffered.  The old
				// selected recipe panel must remain readable while this thread
				// recalculates stored ingredients, result item, and availability.
			}

			public override void PrepareUIZones() { }

			public override void PopulateUIZones() {
				refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
				((CraftingUIState)refreshingUI).RefreshRecipePanel();
			}

			internal void RestoreSelectedPreviewCaches(CraftingSimulationContext context, int amountCraftable, bool isAvailable, bool passesBlock) {
				HasRestoredSelectedPreviewCaches = true;
				RestoredSelectedPreviewContext = context;
				RestoredSelectedPreviewAmountCraftable = amountCraftable;
				RestoredSelectedPreviewIsAvailable = isAvailable;
				RestoredSelectedPreviewPassesBlock = passesBlock;
			}

			private static bool ShouldCollectFullRecipeSnapshots(Recipe recipe)
				=> MagicStorageConfig.IsRecursionEnabled && recipe is not null && recipe.TryGetRecursiveRecipe(out _);
		}

		private class SingleResultRecipeItemsProvider : RecipeItems {
			public readonly IValueProvider<Item> resultItem;
			public bool hasStorageResult;

			public SingleResultRecipeItemsProvider(
				List<Item> staticStoredIngredientsList,
				List<ItemInfo> staticStoredIngredientsInfoList,
				IValueProvider<Item> resultItem
			) : base(
				staticStoredIngredientsList,
				staticStoredIngredientsInfoList
			) {
				this.resultItem = resultItem;
			}

			public override void CopyToStaticCollections() {
				base.CopyToStaticCollections();
				resultItem.OverwriteStatic();
			}

			public override string GetItemCountsReport() {
				return new StringBuilder()
					.Append(base.storedIngredients.items.Count).Append(" stored ingredients and ")
					.Append(resultItem.Value is { IsAir: false } ? "at least one result item" : "no result items")
					.ToString();
			}

			public override void SetResultItem(Item item) {
				if (!base.itemResolver.IsModuleItem(item)) {
					// Items from storage
					resultItem.Value = item;
					hasStorageResult = true;
				} else if (!hasStorageResult && !base.itemResolver.IsInventoryModuleItem(item)) {
					// Items from modules that aren't the Player Inventory modules
					resultItem.Value = item;
				}
			}
		}

		private class CraftResultProvider(IReadOnlyValueProvider<Recipe> selectedRecipe) : IValueProvider<Item> {
			private readonly IReadOnlyValueProvider<Recipe> _selectedRecipe = selectedRecipe;

			public Item StaticSource => CraftingGUI.result;

			public Item Value { get; set; }

			public void ClearStatic() => CraftingGUI.result = null;

			public void CopyFromStatic() => Value = CraftingGUI.result;

			public void CopyToStatic() => CraftingGUI.result = Value ?? (_selectedRecipe.Value is { } recipe ? new Item(recipe.createItem.type, 0) : new Item());
		}

		internal abstract class StandardFilterProvider<T> : IFilterProvider<T> {
			private readonly MainZoneObjectsFilterControls<T> _controls;

			public bool ShowOnlyBlacklisted { get; }

			public StandardFilterProvider(IMainZoneFilterControlsProvider<T> thread) {
				_controls = thread.MainZoneObjectsFilterControls;
				ShowOnlyBlacklisted = MagicStorageConfig.RecipeBlacklistEnabled && thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice == CraftingGUI.RecipeButtonsBlacklistChoice;
			}

			public bool IsFavorited(T value) => _controls.IsFavorited(GetObjectType(value));

			public bool IsHidden(T value) => _controls.IsHidden(GetObjectType(value));

			protected abstract int GetObjectType(T value);
		}

		private class StandardRecipeFilterProvider(IMainZoneFilterControlsProvider<Recipe> thread) : StandardFilterProvider<Recipe>(thread) {
			protected override int GetObjectType(Recipe value) => value.createItem.type;
		}
	}
}
