using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.UI.States;

namespace MagicStorage {
	partial class CraftingGUI {
		private class FullRefreshBuilder : IRefreshThreadBuilder<FullRefreshBuilder> {
			public static FullRefreshBuilder Instance { get; } = new FullRefreshBuilder();

			public StorageViewControls CreateControls() => CreateRefreshThreadControls(MagicUI.craftingUI);

			public RefreshThread CreateThread(StorageViewControls controls) {
				// Full refresh threads must not inherit a pending partial recipe set.
				ClearRecipeRefreshOptimizationState();

				var selectionProvider = new SelectionProvider();

				return new CraftingRefreshThread(
					controls: controls,
					processedStorage: new(
						staticWasModuleItemTable: wasModuleItem,
						staticModuleItemWasFromInventoryTable: moduleItemWasFromInventory,
						staticResultItemsList: items,
						staticResultItemGroupsList: itemGroups,
						staticResultItemsFromModulesList: sourceItemsFromModules,
						staticCountsDictionary: itemCounts,
						staticCountsByPrefixDictionary: itemCountsByPrefix,
						staticCountsHash: itemCountsHash
					),
					mainZoneControls: new(
						zoneObjectFilterChoice: MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>().recipeButtons.Choice,
						favorited: StoragePlayer.LocalPlayer.FavoritedRecipes,
						hidden: StoragePlayer.LocalPlayer.HiddenRecipes,
						configBlacklist: MagicStorageConfig.GlobalRecipeBlacklist
					),
					mainZoneResults: new(
						objectsToRefresh: [],
						staticObjectList: recipes,
						staticAvailableList: recipeAvailable
					),
					ingredientControls: new(
						staticShowAllIngredientsField: new ShowAllIngredientsProvider(((CraftingUIState)MagicUI.craftingUI).recursionButton.IsOn),
						staticInfiniteItemsSet: isItemInfinite,
						staticBlockedList: blockStorageItems,
						staticCreativeUnitField: new CreativeUnitPresentProvider()
					),
					craftingObject: new(
						selection: selectionProvider,
						craftAmountTarget: new CraftAmountTargetProvider()
					),
					availableCache: new(
						staticTable: recipeToAvailableLookup
					),
					simulations: new(
						staticTable: recursionRecipeToAvailableSimulationLookup
					),
					recipeItems: new SingleResultRecipeItemsProvider(
						staticStoredIngredientsList: storageItems,
						staticStoredIngredientsInfoList: storageItemInfo,
						resultItem: new CraftResultProvider(
							selectedRecipe: selectionProvider
						)
					)
				);
			}
		}
	}
}
