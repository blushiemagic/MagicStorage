using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Components;
using MagicStorage.UI.States;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	public static partial class DecraftingGUI {
		internal static readonly ShimmeringRefreshThread NullThread = null;

		internal static readonly List<int> viewingItems = new();
		internal static readonly List<bool> itemAvailable = new();
		internal static int selectedItem = -1;

		// Used to cache the reports for use by StoredIngredientsRefreshThread
		internal static readonly List<ItemReport> cachedShimmerReports = [];

		internal static void Unload() => ClearAllCollections(callCraftingClear: true);

		internal static void ClearAllCollections(bool callCraftingClear) {
			if (callCraftingClear)
				CraftingGUI.ClearAllCollections();

			ResetRefreshCache();
			viewingItems.Clear();
			itemAvailable.Clear();
			resultItems.Clear();
			resultItemsInfo.Clear();
			selectedItem = -1;
		}

		internal static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		internal static TEDecraftingAccess GetDecraftingEntity() => StoragePlayer.LocalPlayer.GetDecraftingAccess();

		internal static Item DoWithdraw(Item toWithdraw, bool toInventory = false) {
			TEStorageHeart heart = GetHeart();
			if (heart is null)
				return new Item();

			using var _ = SecuritySystem.CreateAccessContext();

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = heart.PrepareClientRequest(toInventory ? TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory : TEStorageHeart.Operation.WithdrawThenTryModuleInventory);
				ItemIO.Send(toWithdraw, packet, true, true);
				packet.Send();
				return new Item();
			}

			Item withdrawn = heart.Withdraw(toWithdraw, false);

			if (withdrawn.IsAir)
				withdrawn = CraftingGUI.TryToWithdrawFromModuleItems(heart, toWithdraw, false);

			return withdrawn;
		}

		internal static void SetSelectedItem(int item) {
			NetHelper.Report(true, "Reassigning current item and refreshing recipe panel...");

			CraftingGUI.craftAmountTarget = 1;
			CraftingGUI.blockStorageItems.Clear();

			CreateSelectedItemRefreshThread(item, 1, caller: "DecraftingGUI.SetSelectedItem()").Start();
		}

		public static RefreshThread CreateFullRefreshThread(string caller) {
			var thread = FullRefreshBuilder.Instance.CreateThread();
			thread.SetDebugName($"{caller} thread");
			return thread;
		}

		public static RefreshThread CreateItemListRefreshThread(string caller) {
			// Force all items to be recalculated
			if (MagicUI.ForceNextRefreshToBeFull)
				itemsToRefresh = null;

			var thread = new ItemListRefreshThread(
				controls: CraftingGUI.CreateRefreshThreadControls(MagicUI.decraftingUI),
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
				)
			);

			thread.SetDebugName($"{caller} thread");

			return thread;
		}

		public static RefreshThread CreateSelectedItemRefreshThread(int selectedItem, int craftAmountTarget, string caller) {
			var thread = new ShimmerInfoPanelRefreshThread(
				controls: CraftingGUI.CreateRefreshThreadControls(MagicUI.decraftingUI),
				processedStorage: new(
					staticWasModuleItemTable: CraftingGUI.wasModuleItem,
					staticModuleItemWasFromInventoryTable: CraftingGUI.moduleItemWasFromInventory,
					staticResultItemsList: CraftingGUI.items,
					staticResultItemGroupsList: CraftingGUI.itemGroups,
					staticResultItemsFromModulesList: CraftingGUI.sourceItemsFromModules,
					staticCountsDictionary: CraftingGUI.itemCounts,
					staticCountsByPrefixDictionary: CraftingGUI.itemCountsByPrefix
				),
				ingredientControls: new(
					staticShowAllIngredientsField: new ConstantValueProvider<bool>(false),
					staticInfiniteItemsSet: CraftingGUI.isItemInfinite,
					staticBlockedList:  CraftingGUI.blockStorageItems,
					staticCreativeUnitField: new CraftingGUI.CreativeUnitPresentProvider()
				),
				craftingObject: new(
					selection: new SelectionProvider(selectedItem),
					craftAmountTarget: new CraftingGUI.CraftAmountTargetProvider(craftAmountTarget)
				),
				recipeItems: new ZoneResultsRecipeItemsProvider(
					staticStoredIngredientsList: CraftingGUI.storageItems,
					staticStoredIngredientsInfoList: CraftingGUI.storageItemInfo,
					staticResultItemsList: resultItems,
					staticResultItemsInfoList: resultItemsInfo
				),
				staticReportCacheList: cachedShimmerReports
			);

			thread.SetDebugName($"{caller} thread");

			return thread;
		}
	}
}
