using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading.UI;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader.Config;

namespace MagicStorage {
	partial class DecraftingGUI {
		#region ShimmeringRefreshThread
		internal class ShimmeringRefreshThread : CraftingGUI.CraftingControlsRefreshThread {
			public int selectedItem;
			public readonly HashSet<int> itemsToRefresh;
			public bool[] decraftingRecipeAvailableSnapshot;
			public int[] itemTypeToDecraftRecipeIndexSnapshot;
			public bool[] itemTransmuteAvailableSnapshot;
			public List<ItemReport> cachedShimmerReports;
			public readonly List<int> viewingItems = [];
			public readonly List<bool> viewingItemIsAvailable = [];
			public CraftingGUI.IFilterProvider<int> shimmerableItemFilterProvider;

			public ShimmeringRefreshThread(
				StorageViewControls controls,
				IEnumerable<bool> adjTiles,
				int selectedItem,
				IEnumerable<int> itemsToRefresh,
				int recipeFilter,
				ItemTypeOrderedSet favorited,
				ItemTypeOrderedSet hidden,
				HashSet<ItemDefinition> configBlacklist,
				IEnumerable<ItemData> blockedStoredIngredients,
				int craftAmountTarget
			) : base(controls, adjTiles, null, false, recipeFilter, favorited, hidden, configBlacklist, blockedStoredIngredients, craftAmountTarget) {
				this.selectedItem = selectedItem;
				this.itemsToRefresh = [.. itemsToRefresh];
			}

			protected override void PostItemsFound() {
				AnalyzeIngredients();

				recipeFilterProvider = null;
				shimmerableItemFilterProvider = new StandardShimmerableItemFilterProvider(this);
			}

			protected override void CollectSnapshots() => PopulateShimmerSnapshots(this);

			protected override void PostModuleAccess() {
				if (itemsToRefresh is { Count: > 0 }) {
					// RefreshSpecificItemsAvailablity() will manipulate the current lists, so they need to be cached
					viewingItems.AddRange(DecraftingGUI.viewingItems);
					viewingItemIsAvailable.AddRange(DecraftingGUI.itemAvailable);
				}
			}

			protected override void Execute() {
				DecraftingGUI.SortAndFilter(this);

				base.CopyToStaticCollectionsAndFields();

				DecraftingGUI.selectedItem = selectedItem;

				DecraftingGUI.viewingItems.Clear();
				DecraftingGUI.viewingItems.AddRange(viewingItems);

				DecraftingGUI.itemAvailable.Clear();
				DecraftingGUI.itemAvailable.AddRange(viewingItemIsAvailable);

				DecraftingGUI.cachedShimmerReports.Clear();
				DecraftingGUI.cachedShimmerReports.AddRange(cachedShimmerReports);

				base.PostRefreshRecipes();
			}

			public override void ClearStaticCollections() {
				base.ClearStaticCollections();

				DecraftingGUI.viewingItems.Clear();
				DecraftingGUI.itemAvailable.Clear();
				DecraftingGUI.cachedShimmerReports.Clear();
			}
		}
		#endregion

		#region ShimmerInfoPanelRefreshThread
		internal class ShimmerInfoPanelRefreshThread : CraftingGUI.CommonCraftingThread {
			public int selectedItem;
			public List<ItemReport> cachedShimmerReports;

			public ShimmerInfoPanelRefreshThread(
				StorageViewControls controls,
				int selectedItem,
				IEnumerable<ItemData> blockedStoredIngredients,
				int craftAmountTarget,
				IEnumerable<ItemReport> cachedShimmerReports
			) : base(controls, null, false, blockedStoredIngredients, craftAmountTarget)
			{
				this.selectedItem = selectedItem;
				this.cachedShimmerReports = [.. cachedShimmerReports];
			}

			protected override void CollectObjects() {
				base.CollectObjects();

				base.CopyFromStaticCollectionsAndFields();
			}

			protected override void Execute() {
				DecraftingGUI.RefreshStorageItems(this);

				base.CopyToStaticCollectionsAndFields();

				DecraftingGUI.selectedItem = selectedItem;

				DecraftingGUI.cachedShimmerReports.Clear();
				DecraftingGUI.cachedShimmerReports.AddRange(cachedShimmerReports);
			}
		}
		#endregion

		#region ZoneResultItemsHandler
		internal class ZoneResultItemsHandler(CraftingGUI.CommonCraftingThread thread) : CraftingGUI.IRecipeItemsHandler {
			public readonly CraftingGUI.CommonCraftingThread _thread = thread;
			
			public List<Item> storedIngredients = [];
			public List<ItemInfo> storedIngredientsInfo = [];
			public List<Item> resultItems = [];
			public List<ItemInfo> resultItemsInfo = [];

			public bool FoundStoredResultItem => resultItems.Count > 0;

			public int StoredIngredientCount => storedIngredients.Count;

			public void AddStoredIngredient(Item item) {
				// Items from modules need to be referenced directly
				if (!_thread.wasModuleItem.ContainsKey(item))
					item = item.Clone();

				storedIngredients.Add(item);
				storedIngredientsInfo.Add(item);
			}

			public void CompactCollections() {
				var stored = CraftingGUI.CompactItemList(_thread, this, storedIngredients);
				if (stored.Count != storedIngredients.Count) {
					storedIngredients = stored;
					storedIngredientsInfo = [.. stored.Select(x => new ItemInfo(x))];
				}

				var results = CraftingGUI.CompactItemList(_thread, this, resultItems);
				if (results.Count != resultItems.Count) {
					resultItems = results;
					resultItemsInfo = [.. results.Select(x => new ItemInfo(x))];
				}
			}

			public void CopyToStaticCollections() {
				CraftingGUI.storageItems.AddRange(storedIngredients);
				CraftingGUI.storageItemInfo.AddRange(storedIngredientsInfo);
				DecraftingGUI.resultItems.AddRange(resultItems);
				DecraftingGUI.resultItemsInfo.AddRange(resultItemsInfo);
			}

			public IEnumerable<ItemInfo> GetIngredientsInfo() => storedIngredientsInfo;

			public bool IsItemFromModule(Item item) => _thread.wasModuleItem.ContainsKey(item);

			public void SetResultItem(Item item) {
				if (!_thread.wasModuleItem.ContainsKey(item) && !_thread.moduleItemWasFromInventory.ContainsKey(item)) {
					// Items from storage or modules that aren't the Player Inventory modules
					resultItems.Add(item);
					resultItemsInfo.Add(item);
				}
			}
		}
		#endregion

		#region StandardShimmerableItemFilterProvider
		internal class StandardShimmerableItemFilterProvider(CraftingGUI.CraftingControlsRefreshThread thread) : CraftingGUI.StandardFilterProvider<int>(thread) {
			protected override int GetObjectType(int value) => value;
		}
		#endregion
	}
}
