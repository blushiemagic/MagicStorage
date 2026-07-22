using MagicStorage.Common.Systems;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using MagicStorage.CrossMod;
using MagicStorage.UI.States;

namespace MagicStorage {
	partial class DecraftingGUI {
		internal static bool hasSlotFocus;
		internal static int slotFocus = -1;

		internal static int rightClickTimer;
		internal static int maxRightClickTimer = CraftingGUI.StartMaxRightClickTimer;

		internal static void ResetSlotFocus()
		{
			hasSlotFocus = false;
			slotFocus = -1;
			rightClickTimer = 0;
			maxRightClickTimer = CraftingGUI.StartMaxRightClickTimer;
			CraftingUIState.ClearSlotFocusSourceSlot();
		}

		internal static void SlotFocusLogic()
		{
			if (MagicUI.CurrentlyRefreshing)
				return;  // Delay logic until threading stops

			if (MagicStorageConfig.UseOldRightClickSlotFocus)
				SlotFocusLogic_0();
			else
				SlotFocusLogic_1();
		}

		private static void SlotFocusLogic_0()
		{
			if (!hasSlotFocus || slotFocus == -1 || slotFocus >= resultItems.Count || !Main.mouseItem.IsAir && (!StorageAggregator.CanCombineItems(Main.mouseItem, resultItems[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack)) {
				ResetSlotFocus();
			} else {
				if (rightClickTimer <= 0) {
					rightClickTimer = maxRightClickTimer;
					maxRightClickTimer = maxRightClickTimer * 3 / 4;
					if (maxRightClickTimer <= 0)
						maxRightClickTimer = 1;
					Item toWithdraw = resultItems[slotFocus].Clone();
					toWithdraw.stack = 1;
					Item result = DoWithdraw(toWithdraw);
					if (result is null || result.IsAir) {
						ResetSlotFocus();
						return;
					}

					if (Main.mouseItem.IsAir)
						Main.mouseItem = result;
					else {
						Utility.CallOnStackHooks(Main.mouseItem, result, result.stack);

						Main.mouseItem.stack += result.stack;
					}

					CraftingUIState.ConsumeSlotFocusResultPreview(result.stack);
					MagicUI.RequestFullRefresh();
					SetNextDefaultItemCollectionToRefresh(Main.mouseItem.type);
					SoundEngine.PlaySound(SoundID.MenuTick);
				}

				rightClickTimer--;
			}
		}

		private static void SlotFocusLogic_1()
		{
			if (slotFocus == -1
			|| slotFocus >= resultItems.Count
			|| !ItemStackSplitting.TickOneSplitOntoMouse(resultItems[slotFocus], CloneWithStackOverride, out bool waitingForNextSplit, withdrawn => CraftingUIState.ConsumeSlotFocusResultPreview(withdrawn.stack))
			|| !waitingForNextSplit)
			{
				ResetSlotFocus();
			}
			else
			{
				MagicUI.RequestFullRefresh();
				SetNextDefaultItemCollectionToRefresh(Main.mouseItem.type);
			}
		}

		private static Item CloneWithStackOverride(Item original, int stack) {
			Item clone = original.Clone();
			clone.stack = stack;
			return DoWithdraw(clone);
		}
	}
}
