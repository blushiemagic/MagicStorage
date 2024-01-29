using MagicStorage.Common.Systems;
using Terraria.Audio;
using Terraria.ID;
using Terraria;

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
		}

		internal static void SlotFocusLogic()
		{
			if (MagicUI.CurrentlyRefreshing)
				return;  // Delay logic until threading stops

			if (!hasSlotFocus || slotFocus == -1 || slotFocus >= resultItems.Count || !Main.mouseItem.IsAir && (!ItemCombining.CanCombineItems(Main.mouseItem, resultItems[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack)) {
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
					if (Main.mouseItem.IsAir)
						Main.mouseItem = result;
					else {
						Utility.CallOnStackHooks(Main.mouseItem, result, result.stack);

						Main.mouseItem.stack += result.stack;
					}

					MagicUI.SetRefresh();
					SoundEngine.PlaySound(SoundID.MenuTick);
				}

				rightClickTimer--;
			}
		}
	}
}
