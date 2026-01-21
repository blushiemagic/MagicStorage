using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.UI.States;
using System.Collections.Generic;
using System.Text;
using Terraria;

namespace MagicStorage {
	partial class DecraftingGUI {
		public class ShimmeringRefreshThread : RefreshThread, IStorageItemsPovider, IProcessedStorageItemsProvider, IMainZoneFilterControlsProvider<int>, IMainZoneObjectResultsProvider<int>, IIngredientControlsProvider, ICraftingObjectProvider<int>, IShimmerSnapshotsProvider, IShimmerItemReportsProvider, IRecipeItemsProvider {
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

			public RecipeItems RecipeItems { get; }

			public ShimmeringRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				MainZoneObjectsFilterControls<int> mainZoneControls,
				MainZoneObjectResults<int> mainZoneResults,
				IngredientControls ingredientControls,
				CraftingObject<int> craftingObject,
				RecipeItems recipeItems,
				List<ItemReport> staticReportCacheList
			) : base(MagicUI.decraftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				MainZoneObjectsFilterControls = mainZoneControls;
				MainZoneObjectsResults = mainZoneResults;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				ShimmerItemReports = new(staticReportCacheList);
				RecipeItems = recipeItems;
				RecipeItems.itemResolver = ProcessedStorageItems.CreateItemResolver();
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

				InitTaskSchedule(6, "Updating Caches");

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				CompleteOne();
				MainZoneObjectsResults.CopyToStaticCollections();
				CompleteOne();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CompleteOne();
				CraftingObject.CopyToStaticFields();
				CompleteOne();
				ShimmerItemReports.CopyToStaticCollection();
				CompleteOne();
				RecipeItems.CopyToStaticCollections();
				CompleteOne();

				StorageGUI.hasAnyErrorItems = base.foundErrorItem;
				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;

				CraftingGUI.hasCompleteData = true;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				MainZoneObjectsResults.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
				ShimmerItemReports.ClearStaticCollection();
				RecipeItems.ClearStaticCollections();
				StorageGUI.hasAnyErrorItems = false;
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
				CraftingGUI.hasCompleteData = false;

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

				InitTaskSchedule(3, "Updating Caches");

				ProcessedStorageItems.CopyToStaticCollectionsAndFields();
				CompleteOne();
				MainZoneObjectsResults.CopyToStaticCollections();
				CompleteOne();
				IngredientControls.CopyToStaticCollectionsAndFields();
				CompleteOne();

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
				
				CraftingGUI.hasCompleteData = true;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				ProcessedStorageItems.ClearStaticCollections();
				MainZoneObjectsResults.ClearStaticCollections();
				IngredientControls.ClearStaticCollections();
			}

			public override void PrepareUIZones() => refreshingUI.GetDefaultPage().OnRefreshStart();

			public override void PopulateUIZones() => refreshingUI.GetDefaultPage().Refresh();
		}

		public class ShimmerInfoPanelRefreshThread : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, ICraftingObjectProvider<int>, IShimmerItemReportsProvider, IRecipeItemsProvider {
			public override bool IsPartialThread => true;

			public override bool HasCompleteData => CraftingGUI.hasCompleteData;

			public override IRefreshThreadBuilder FullRefreshBuilder => DecraftingGUI.FullRefreshBuilder.Instance;

			public ProcessedStorageItems ProcessedStorageItems { get; }

			public IngredientControls IngredientControls { get; }

			public CraftingObject<int> CraftingObject { get; }

			public ShimmerItemReports ShimmerItemReports { get; }

			public RecipeItems RecipeItems { get; }

			public ShimmerInfoPanelRefreshThread(
				StorageViewControls controls,
				ProcessedStorageItems processedStorage,
				IngredientControls ingredientControls,
				CraftingObject<int> craftingObject,
				RecipeItems recipeItems,
				List<ItemReport> staticReportCacheList
			) : base(MagicUI.decraftingUI, controls) {
				ProcessedStorageItems = processedStorage;
				IngredientControls = ingredientControls;
				CraftingObject = craftingObject;
				ShimmerItemReports = new(staticReportCacheList);
				RecipeItems = recipeItems;
				RecipeItems.itemResolver = ProcessedStorageItems.CreateItemResolver();
			}

			protected override void CollectObjects() {
				CraftingGUI.hasCompleteData = false;

				ProcessedStorageItems.CopyFromStaticCollectionsAndFields();
				IngredientControls.CollectObjects(this);
				ShimmerItemReports.CopyFromStaticCollection();
			}

			protected override void Execute() {
				DecraftingGUI.RefreshStorageItems(this);

				InitTaskSchedule(4, "Updating Caches");

				IngredientControls.CopyToStaticCollectionsAndFields();
				CompleteOne();
				CraftingObject.CopyToStaticFields();
				CompleteOne();
				ShimmerItemReports.CopyToStaticCollection();
				CompleteOne();
				RecipeItems.CopyToStaticCollections();
				CompleteOne();
				
				CraftingGUI.hasCompleteData = true;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() {
				IngredientControls.ClearStaticCollections();
				ShimmerItemReports.ClearStaticCollection();
				RecipeItems.ClearStaticCollections();
			}

			public override void PrepareUIZones() {
				refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
				((DecraftingUIState)refreshingUI).ClearRecipePanelZones();
			}

			public override void PopulateUIZones() {
				refreshingUI.GetDefaultPage<BaseStorageUIAccessPage>().PopulateMainZone();
				((DecraftingUIState)refreshingUI).RefreshRecipePanel();
			}
		}

		private class ZoneResultsRecipeItemsProvider : RecipeItems {
			public readonly ItemInfoListProvider results;

			public ZoneResultsRecipeItemsProvider(
				List<Item> staticStoredIngredientsList,
				List<ItemInfo> staticStoredIngredientsInfoList,
				List<Item> staticResultItemsList,
				List<ItemInfo> staticResultItemsInfoList
			) : base(
				staticStoredIngredientsList,
				staticStoredIngredientsInfoList
			) {
				results = new(
					staticItemsList: staticResultItemsList,
					staticInfoList: staticResultItemsInfoList
				);
			}

			public override void CompactCollections(RefreshThread thread) {
				base.CompactCollections(thread);

				CraftingGUI.CompactItemList(
					thread,
					results,
					itemResolver.IsModuleItem,
					"Result Items"
				);
			}

			public override void CopyFromStaticCollections() {
				base.CopyFromStaticCollections();
				results.items.CopyFromStatic();
				results.info.CopyFromStatic();
			}

			public override void CopyToStaticCollections() {
				base.CopyToStaticCollections();
				results.items.CopyToStatic();
				results.info.CopyToStatic();
			}

			public override void ClearStaticCollections() {
				base.ClearStaticCollections();
				results.items.ClearStatic();
				results.info.ClearStatic();
			}

			public override string GetItemCountsReport() {
				return new StringBuilder()
					.Append(base.storedIngredients.items.Count).Append(" stored ingredients and ")
					.Append(results.items.Count > 0 ? results.items.Count.ToString() : "no").Append(" result items")
					.ToString();
			}

			public override void SetResultItem(Item item) {
				if (!base.itemResolver.IsModuleItem(item) || !base.itemResolver.IsInventoryModuleItem(item)) {
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
