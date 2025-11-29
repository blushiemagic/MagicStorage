using MagicStorage.Components;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;

namespace MagicStorage {
	public static partial class DecraftingGUI {
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
				withdrawn = CraftingGUI.TryToWithdrawFromModuleItems(toWithdraw, false);

			return withdrawn;
		}

		internal static void SetSelectedItem(int item) {
			NetHelper.Report(true, "Reassigning current item and refreshing recipe panel...");

			CraftingGUI.craftAmountTarget = 1;
			CraftingGUI.blockStorageItems.Clear();

			CreateSelectedItemRefreshThread(item, caller: nameof(SetSelectedItem)).Start();
		}

		internal static ShimmerInfoPanelRefreshThread CreateSelectedItemRefreshThread(int selectedItem, string caller) {
			CraftingGUI.GetCommonRefreshThreadParameters(out _, out var blockedStoredIngredients, out var craftAmountTarget);

			var thread = new ShimmerInfoPanelRefreshThread(
				controls: CreateRefreshThreadControls(),
				selectedItem: selectedItem,
				blockedStoredIngredients: blockedStoredIngredients,
				craftAmountTarget: craftAmountTarget,
				cachedShimmerReports: cachedShimmerReports
			);
			thread.SetDebugName($"DecraftingGUI.{caller} thread:");

			return thread;
		}
	}
}
