using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.UI.States;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		public class CraftingRefreshThread : RefreshThread, IStorageItemsPovider, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, IIngredientControlsProvider, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSnapshotsProvider {
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

			public RecipeSnapshots RecipeSnapshots { get; } = new();

			public CraftingRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<Recipe> mainZoneControls,
				MainZoneObjectResults<Recipe> mainZoneResults,
				IngredientControls ingredientControls,
				CraftingObject<Recipe> craftingObject,
				CraftObjectAvailableCache<Recipe> availableCache
			) : base(MagicUI.craftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				MainZoneObjectsResults = mainZoneResults;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				CraftObjectAvailableCache = availableCache;
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
				
				MainZoneObjectsFilterControls.adjTiles = [.. CraftingGUI.adjTiles];
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

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				MainZoneObjectsResults.CopyToStaticCollections();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CraftingObject.CopyToStaticFields();
				CraftObjectAvailableCache.CopyToStaticCollection();

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
				CraftingGUI.lastKnownRecursionErrorForStoredItems = base.storedItemsError;

				CraftingGUI.SetRecipeAndCraftingCaches(this);

				CraftingGUI.hasCompleteData = true;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				MainZoneObjectsResults.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
				CraftObjectAvailableCache.ClearStaticCollection();
			}

			// Unused due to being a full thread
			public override void PrepareUIZones() { }

			public override void PopulateUIZones() { }
		}

		public class RecipeListRefreshThread : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider<Recipe>, IMainZoneObjectResultsProvider<Recipe>, ICraftingObjectProvider<Recipe>, ICraftObjectAvailableCacheProvider<Recipe>, IRecipeSnapshotsProvider {
			public bool[] recipeConditionsMetSnapshot;

			public override bool IsPartialThread => true;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => CraftingGUI.FullRefreshBuilder.Instance;

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public MainZoneObjectsFilterControls<Recipe> MainZoneObjectsFilterControls { get; }

			public MainZoneObjectResults<Recipe> MainZoneObjectsResults { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<Recipe> CraftingObject { get; }

			public CraftObjectAvailableCache<Recipe> CraftObjectAvailableCache { get; }

			public RecipeSnapshots RecipeSnapshots { get; } = new();

			public RecipeListRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<Recipe> mainZoneControls,
				MainZoneObjectResults<Recipe> mainZoneResults,
				IngredientControls ingredientControls,
				CraftingObject<Recipe> craftingObject,
				CraftObjectAvailableCache<Recipe> availableCache
			) : base(MagicUI.craftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				MainZoneObjectsResults = mainZoneResults;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				CraftObjectAvailableCache = availableCache;
			}

			protected override void CollectObjects() {
				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PreRefreshRecipes(sandbox);

				ProcessedStorageItems.CopyFromStaticCollectionsAndFields();
				IngredientControls.CopyFromStaticCollectionsAndFields();
				MainZoneObjectsResults.CollectObjects();

				AnalyzeIngredients();
				
				MainZoneObjectsFilterControls.adjTiles = [.. CraftingGUI.adjTiles];
				MainZoneObjectsFilterControls.filterProvider = new StandardRecipeFilterProvider(this);

				RecipeSnapshots.CollectObjects();

				foreach (EnvironmentModule module in base.Heart.GetModules())
					module.ResetPlayer(sandbox);

				CraftingGUI.ResetRecentRecipeCache();
			}

			protected override void Execute() {
				CraftingGUI.RefreshRecipes(this);

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				foreach (var module in base.Heart.GetModules())
					module.PostRefreshRecipes(sandbox);

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				MainZoneObjectsResults.CopyToStaticCollections();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CraftingObject.CopyToStaticFields();
				CraftObjectAvailableCache.CopyToStaticCollection();

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
			//	CraftingGUI.lastKnownRecursionErrorForStoredItems = base.storedItemsError;

				CraftingGUI.SetRecipeAndCraftingCaches(this);

				CraftingGUI.hasCompleteData = true;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				MainZoneObjectsResults.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
				CraftObjectAvailableCache.ClearStaticCollection();
			}

			public override void PrepareUIZones() => refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().slotZone.ClearContexts();

			public override void PopulateUIZones() => refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
		}

		public class RecipeInfoPanelRefreshThread : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, ICraftingObjectProvider<Recipe> {
			public override bool IsPartialThread => true;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => CraftingGUI.FullRefreshBuilder.Instance;

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<Recipe> CraftingObject { get; }

			public RecipeInfoPanelRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				IngredientControls ingredientControls,
				CraftingObject<Recipe> craftingObject
			) : base(MagicUI.craftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
			}

			protected override void CollectObjects() {
				ProcessedStorageItems.CopyFromStaticCollectionsAndFields();
				IngredientControls.CopyFromStaticCollectionsAndFields();
			}

			protected override void Execute() {
				CraftingGUI.RefreshStorageItems(this);
				
				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CraftingObject.CopyToStaticFields();
				
				CraftingGUI.lastKnownRecursionErrorForStoredItems = base.storedItemsError;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
			}

			public override void PrepareUIZones() {
				refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().slotZone.ClearContexts();
				((CraftingUIState)refreshingUI).ClearRecipePanelZones();
			}

			public override void PopulateUIZones() {
				refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
				((CraftingUIState)refreshingUI).PopulateRecipePanelZones();
			}
		}

		private class SingleResultItemHandler<T> : IRecipeItemsHandler
			where T : RefreshThread, IProcessedStorageItemsProvider
		{
			public readonly T _thread;

			public ListProvider<Item> storedIngredients;
			public ListProvider<ItemInfo> storedIngredientsInfo;
			public IValueProvider<Item> resultItem;
			public bool hasStorageResult;

			public bool FoundStoredResultItem => resultItem.Value is { IsAir: false };

			public int StoredIngredientCount => storedIngredients.Count;

			public SingleResultItemHandler(
				T thread,
				List<Item> staticStoredIngredientsList,
				List<ItemInfo> staticStoredIngredientsInfoList,
				IValueProvider<Item> resultItem
			) {
				_thread = thread;
				storedIngredients = new ListProvider<Item>(staticStoredIngredientsList);
				storedIngredientsInfo = new ListProvider<ItemInfo>(staticStoredIngredientsInfoList);
				this.resultItem = resultItem;
			}

			public void AddStoredIngredient(Item item) {
				// Items from modules need to be referenced directly
				if (!_thread.ProcessedStorageItems.wasModuleItem.ContainsKey(item))
					item = item.Clone();

				storedIngredients.Add(item);
				storedIngredientsInfo.Add(item);
			}

			public void CompactCollections() {
				var stored = CraftingGUI.CompactItemList(_thread, this, storedIngredients.Value);
				if (stored.Count != storedIngredients.Count) {
					storedIngredients.Clear();
					storedIngredients.AddRange(stored);
					storedIngredientsInfo.Clear();
					storedIngredientsInfo.AddRange(stored.Select(x => new ItemInfo(x)));
				}
			}

			public void CopyToStaticCollections() {
				storedIngredients.OverwriteStatic();
				storedIngredientsInfo.OverwriteStatic();
				resultItem.OverwriteStatic();
			}

			public IEnumerable<ItemInfo> GetIngredientsInfo() => storedIngredientsInfo;

			public bool IsItemFromModule(Item item) => _thread.ProcessedStorageItems.wasModuleItem.ContainsKey(item);

			public void SetResultItem(Item item) {
				if (!_thread.ProcessedStorageItems.wasModuleItem.ContainsKey(item)) {
					// Items from storage
					resultItem.Value = item;
					hasStorageResult = true;
				} else if (!hasStorageResult && !_thread.ProcessedStorageItems.moduleItemWasFromInventory.ContainsKey(item)) {
					// Items from modules that aren't the Player Inventory modules
					resultItem.Value = item;
				}
			}
		}

		private class CraftResultProvider(IReadOnlyValueProvider<Recipe> selectedRecipe) : IValueProvider<Item> {
			private readonly IReadOnlyValueProvider<Recipe> _selectedRecipe = selectedRecipe;

			public Item Value { get; set; }

			public void ClearStatic() => CraftingGUI.result = null;

			public void CopyFromStatic() => Value = CraftingGUI.result;

			public void CopyToStatic() => CraftingGUI.result = Value ?? new Item(_selectedRecipe.Value.createItem.type, 0);
		}

		internal abstract class StandardFilterProvider<T> : IFilterProvider<T> {
			private readonly IMainZoneFilterControlsProvider<T> _thread;

			public bool ShowOnlyBlacklisted { get; }

			public StandardFilterProvider(IMainZoneFilterControlsProvider<T> thread) {
				_thread = thread;
				ShowOnlyBlacklisted = MagicStorageConfig.RecipeBlacklistEnabled && thread.MainZoneObjectsFilterControls.zoneObjectFilterChoice == CraftingGUI.RecipeButtonsBlacklistChoice;
			}

			public bool IsFavorited(T value) => _thread.MainZoneObjectsFilterControls.IsFavorited(GetObjectType(value));

			public bool IsHidden(T value) => _thread.MainZoneObjectsFilterControls.IsHidden(GetObjectType(value));

			protected abstract int GetObjectType(T value);
		}

		private class StandardRecipeFilterProvider(IMainZoneFilterControlsProvider<Recipe> thread) : StandardFilterProvider<Recipe>(thread) {
			protected override int GetObjectType(Recipe value) => value.createItem.type;
		}
	}
}
