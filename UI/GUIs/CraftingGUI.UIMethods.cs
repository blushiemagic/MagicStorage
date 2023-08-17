using Terraria.Audio;
using Terraria.ID;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static bool slotFocus;

		internal static int rightClickTimer;
		internal static int maxRightClickTimer = StartMaxRightClickTimer;

		internal static void SlotFocusLogic()
		{
			if (StorageGUI.CurrentlyRefreshing)
				return;  // Delay logic until threading stops

			if (result == null || result.IsAir || !Main.mouseItem.IsAir && (!ItemCombining.CanCombineItems(Main.mouseItem, result) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
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
					
					StorageGUI.SetRefresh();
				}

				rightClickTimer--;
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
