using Terraria.Audio;
using Terraria.ID;
using Terraria;

namespace MagicStorage {
	partial class StorageGUI {
		internal static int slotFocus = -1;

		private static int rightClickTimer;
		private static int maxRightClickTimer = startMaxRightClickTimer;

		internal static bool itemDeletionMode;
		internal static int itemDeletionSlotFocus = -1;

		internal static void ResetSlotFocus()
		{
			slotFocus = -1;
			rightClickTimer = 0;
			maxRightClickTimer = startMaxRightClickTimer;
		}

		internal static void SlotFocusLogic()
		{
			if (CurrentlyRefreshing)
				return;  // Delay logic until threading stops

			if (slotFocus >= items.Count ||
				!Main.mouseItem.IsAir && (!ItemCombining.CanCombineItems(Main.mouseItem, items[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
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
					Item toWithdraw = items[slotFocus].Clone();
					toWithdraw.stack = 1;
					Item result = DoWithdraw(toWithdraw);
					if (Main.mouseItem.IsAir)
						Main.mouseItem = result;
					else {
						Utility.CallOnStackHooks(Main.mouseItem, result, result.stack);

						Main.mouseItem.stack += result.stack;
					}

					SetRefresh();
					SoundEngine.PlaySound(SoundID.MenuTick);
				}

				rightClickTimer--;
			}
		}
	}
}
