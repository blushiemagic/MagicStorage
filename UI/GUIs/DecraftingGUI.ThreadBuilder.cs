using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.UI.States;

namespace MagicStorage {
	partial class DecraftingGUI {
		private class FullRefreshBuilder : IRefreshThreadBuilder<FullRefreshBuilder> {
			public static FullRefreshBuilder Instance { get; } = new FullRefreshBuilder();

			public StorageViewControls CreateControls() => CraftingGUI.CreateRefreshThreadControls(MagicUI.decraftingUI);

			public RefreshThread CreateThread(StorageViewControls controls) {
				// Force all items to be recalculated
				if (MagicUI.IgnoreSpecificZoneRefreshing)
					itemsToRefresh = null;

				return new ShimmeringRefreshThread(
					controls: controls,
					processedStorage: new(
						staticWasModuleItemTable: CraftingGUI.wasModuleItem,
						staticModuleItemWasFromInventoryTable: CraftingGUI.moduleItemWasFromInventory,
						staticResultItemsList: CraftingGUI.items,
						staticResultItemGroupsList: CraftingGUI.itemGroups,
						staticResultItemsFromModulesList: CraftingGUI.sourceItemsFromModules,
						staticCountsDictionary: CraftingGUI.itemCounts,
						staticCountsByPrefixDictionary: CraftingGUI.itemCountsByPrefix
					),
					mainZoneControls: new(
						zoneObjectFilterChoice: MagicUI.decraftingUI.GetDefaultPage<DecraftingUIState.ShimmeringPage>().recipeButtons.Choice,
						favorited: StoragePlayer.LocalPlayer.FavoritedShimmerItems,
						hidden: StoragePlayer.LocalPlayer.HiddenShimmerItems,
						configBlacklist: MagicStorageConfig.GlobalShimmerItemBlacklist
					),
					mainZoneResults: new(
						objectsToRefresh: itemsToRefresh,
						staticObjectList: viewingItems,
						staticAvailableList: itemAvailable
					),
					ingredientControls: new(
						staticShowAllIngredientsField: new ConstantValueProvider<bool>(false),
						staticInfiniteItemsSet: CraftingGUI.isItemInfinite,
						staticBlockedList:  CraftingGUI.blockStorageItems,
						staticCreativeUnitField: new CraftingGUI.CreativeUnitPresentProvider()
					),
					craftingObject: new(
						selection: new SelectionProvider(),
						craftAmountTarget: new CraftingGUI.CraftAmountTargetProvider()
					),
					recipeItems: new ZoneResultsRecipeItemsProvider(
						staticStoredIngredientsList: CraftingGUI.storageItems,
						staticStoredIngredientsInfoList: CraftingGUI.storageItemInfo,
						staticResultItemsList: resultItems,
						staticResultItemsInfoList: resultItemsInfo
					),
					staticReportCacheList: cachedShimmerReports
				);
			}
		}
	}
}
