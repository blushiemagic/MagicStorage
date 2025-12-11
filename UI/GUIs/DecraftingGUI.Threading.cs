using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.UI.States;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage {
	partial class DecraftingGUI {
		public class ShimmeringRefreshThread : RefreshThread, IStorageItemsPovider, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<int>, IMainZoneObjectResultsProvider<int>, IIngredientControlsProvider, ICraftingObjectProvider<int>, IShimmerSnapshotsProvider, IShimmerItemReportsProvider {
			public override bool IsPartialThread => false;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => DecraftingGUI.FullRefreshBuilder.Instance;

			public StorageItems StorageItems { get; } = new();

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public MainZoneObjectsFilterControls<int> MainZoneObjectsFilterControls { get; }

			public MainZoneObjectResults<int> MainZoneObjectsResults { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<int> CraftingObject { get; }

			public ShimmerSnapshots ShimmerSnapshots { get; } = new();

			public ShimmerItemReports ShimmerItemReports { get; }

			public ShimmeringRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<int> mainZoneControls,
				MainZoneObjectResults<int> mainZoneResults,
				IngredientControls ingredientControls,
				CraftingObject<int> craftingObject,
				List<ItemReport> staticReportCacheList
			) : base(MagicUI.decraftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				MainZoneObjectsResults = mainZoneResults;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				ShimmerItemReports = new(staticReportCacheList);
			}

			protected override void CollectObjects() {
				CraftingGUI.hasCompleteData = false;

				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				StorageItems.CollectObjects(this);
				ProcessedStorageItems.CollectObjects(this);
				IngredientControls.CollectObjects(this);
				MainZoneObjectsResults.CollectObjects();
				ShimmerItemReports.CollectObjects(CraftingObject.selection.Value);

				AnalyzeIngredients();

				MainZoneObjectsFilterControls.adjTiles = [.. CraftingGUI.adjTiles];
				MainZoneObjectsFilterControls.filterProvider = new StandardShimmerableItemFilterProvider(this);

				ShimmerSnapshots.CollectObjects();
			}

			protected override void Execute() {
				DecraftingGUI.SortAndFilter(this);

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				MainZoneObjectsResults.CopyToStaticCollections();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CraftingObject.CopyToStaticFields();
				ShimmerItemReports.CopyToStaticCollection();

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;

				CraftingGUI.hasCompleteData = true;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				MainZoneObjectsResults.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
				ShimmerItemReports.ClearStaticCollection();
			}

			// Unused due to being a full thread
			public override void PrepareUIZones() { }

			public override void PopulateUIZones() { }
		}

		public class ItemListRefreshThread : RefreshThread, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<int>, IMainZoneObjectResultsProvider<int>, IIngredientControlsProvider, IShimmerSnapshotsProvider {
			public override bool IsPartialThread => true;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => DecraftingGUI.FullRefreshBuilder.Instance;

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public MainZoneObjectsFilterControls<int> MainZoneObjectsFilterControls { get; }

			public MainZoneObjectResults<int> MainZoneObjectsResults { get; }

			public IngredientControls IngredientControls { get; }

			public ShimmerSnapshots ShimmerSnapshots { get; } = new();

			public ItemListRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<int> mainZoneControls,
				MainZoneObjectResults<int> mainZoneResults,
				IngredientControls ingredientControls
			) : base(MagicUI.decraftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				MainZoneObjectsResults = mainZoneResults;
				IngredientControls = ingredientControls;
			}

			protected override void CollectObjects() {
				var sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				ProcessedStorageItems.CopyFromStaticCollectionsAndFields();
				IngredientControls.CopyFromStaticCollectionsAndFields();
				MainZoneObjectsResults.CollectObjects();

				AnalyzeIngredients();
				
				MainZoneObjectsFilterControls.adjTiles = [.. CraftingGUI.adjTiles];
				MainZoneObjectsFilterControls.filterProvider = new StandardShimmerableItemFilterProvider(this);

				ShimmerSnapshots.CollectObjects();
			}

			protected override void Execute() {
				DecraftingGUI.RefreshItemsAvailability(this);

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				MainZoneObjectsResults.CopyToStaticCollections();
				IngredientControls.CopyToStaticCollectionsAndFields();

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				MainZoneObjectsResults.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
			}

			public override void PrepareUIZones() => refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().slotZone.ClearContexts();

			public override void PopulateUIZones() => refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
		}

		public class ShimmerInfoPanelRefreshThread : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, ICraftingObjectProvider<int>, IShimmerItemReportsProvider {
			public override bool IsPartialThread => true;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => DecraftingGUI.FullRefreshBuilder.Instance;

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<int> CraftingObject { get; }

			public ShimmerItemReports ShimmerItemReports { get; }

			public ShimmerInfoPanelRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				IngredientControls ingredientControls,
				CraftingObject<int> craftingObject,
				List<ItemReport> staticReportCacheList
			) : base(MagicUI.decraftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				ShimmerItemReports = new(staticReportCacheList);
			}

			protected override void CollectObjects() {
				ProcessedStorageItems.CopyFromStaticCollectionsAndFields();
				IngredientControls.CollectObjects(this);
				ShimmerItemReports.CopyFromStaticCollection();
			}

			protected override void Execute() {
				DecraftingGUI.RefreshStorageItems(this);

				IngredientControls.CopyToStaticCollectionsAndFields();
				CraftingObject.CopyToStaticFields();
				ShimmerItemReports.CopyToStaticCollection();
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				IngredientControls.ClearStaticCollections();
				ShimmerItemReports.ClearStaticCollection();
			}

			public override void PrepareUIZones() {
				refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
				((DecraftingUIState)refreshingUI).ClearRecipePanelZones();
			}

			public override void PopulateUIZones() {
				refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
				((DecraftingUIState)refreshingUI).PopulateRecipePanelZones();
			}
		}

		private class ZoneResultItemsHandler<T> : IRecipeItemsHandler
			where T : RefreshThread, IProcessedStorageItemsProvider
		{
			public readonly T _thread;

			public readonly ListProvider<Item> storedIngredients;
			public readonly ListProvider<ItemInfo> storedIngredientsInfo;
			public readonly ListProvider<Item> resultItems;
			public readonly ListProvider<ItemInfo> resultItemsInfo;

			public bool FoundStoredResultItem => resultItems.Count > 0;

			public int StoredIngredientCount => storedIngredients.Count;

			public ZoneResultItemsHandler(
				T thread,
				List<Item> staticStoredIngredientsList,
				List<ItemInfo> staticStoredIngredientsInfoList,
				List<Item> staticResultItemsList,
				List<ItemInfo> staticResultItemsInfoList
			) {
				_thread = thread;
				storedIngredients = new(staticStoredIngredientsList);
				storedIngredientsInfo = new(staticStoredIngredientsInfoList);
				resultItems = new(staticResultItemsList);
				resultItemsInfo = new(staticResultItemsInfoList);
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

				var results = CraftingGUI.CompactItemList(_thread, this, resultItems.Value);
				if (results.Count != resultItems.Count) {
					resultItems.Clear();
					resultItems.AddRange(results);
					resultItemsInfo.Clear();
					resultItemsInfo.AddRange(results.Select(x => new ItemInfo(x)));
				}
			}

			public void CopyToStaticCollections() {
				storedIngredients.OverwriteStatic();
				storedIngredientsInfo.OverwriteStatic();
				resultItems.OverwriteStatic();
				resultItemsInfo.OverwriteStatic();
			}

			public IEnumerable<ItemInfo> GetIngredientsInfo() => storedIngredientsInfo;

			public bool IsItemFromModule(Item item) => _thread.ProcessedStorageItems.wasModuleItem.ContainsKey(item);

			public void SetResultItem(Item item) {
				if (!_thread.ProcessedStorageItems.wasModuleItem.ContainsKey(item) && !_thread.ProcessedStorageItems.moduleItemWasFromInventory.ContainsKey(item)) {
					// Items from storage or modules that aren't the Player Inventory modules
					resultItems.Add(item);
					resultItemsInfo.Add(item);
				}
			}
		}

		private class StandardShimmerableItemFilterProvider(IMainZoneFilterControlsProvider<int> thread) : CraftingGUI.StandardFilterProvider<int>(thread) {
			protected override int GetObjectType(int value) => value;
		}
	}
}
