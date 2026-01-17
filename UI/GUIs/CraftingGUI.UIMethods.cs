using Terraria.Audio;
using Terraria.ID;
using Terraria;
using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static bool slotFocus;

		internal static int rightClickTimer;
		internal static int maxRightClickTimer = StartMaxRightClickTimer;

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
			if (!slotFocus || result == null || result.IsAir || !Main.mouseItem.IsAir && (!StorageAggregator.CanCombineItems(Main.mouseItem, result) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
			{
				ResetSlotFocus();
			}
			else
			{
				if (rightClickTimer <= 0)
				{
					rightClickTimer = maxRightClickTimer;
					maxRightClickTimer = maxRightClickTimer * 3 / 4;
					if (maxRightClickTimer <= 0)
						maxRightClickTimer = 1;
					Item withdrawn = DoWithdrawResult(1);
					if (Main.mouseItem.IsAir)
						Main.mouseItem = withdrawn;
					else {
						Utility.CallOnStackHooks(Main.mouseItem, withdrawn, withdrawn.stack);

						Main.mouseItem.stack += withdrawn.stack;
					}

					SoundEngine.PlaySound(SoundID.MenuTick);
					
					MagicUI.RequestFullRefresh();
					SetNextDefaultRecipeCollectionToRefresh(Main.mouseItem.type);
				}

				rightClickTimer--;
			}
		}

		private static void SlotFocusLogic_1()
		{
			if (!slotFocus
			|| result is not { IsAir: false }
			|| !ItemStackSplitting.TickOneSplitOntoMouse(result, (_, stack) => DoWithdrawResult(stack), out bool waitingForNextSplit)
			|| !waitingForNextSplit)
			{
				ResetSlotFocus();
			}
			else
			{
				MagicUI.RequestFullRefresh();
				SetNextDefaultRecipeCollectionToRefresh(Main.mouseItem.type);
			}
		}

		internal static void ResetSlotFocus()
		{
			slotFocus = false;
			rightClickTimer = 0;
			maxRightClickTimer = StartMaxRightClickTimer;
		}
	}
}
