using Terraria.Audio;
using Terraria.ID;
using Terraria;
using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;

namespace MagicStorage {
	partial class StorageGUI {
		public enum ActionMode {
			/// <summary>
			/// Standard behavior.
			/// </summary>
			Normal,
			/// <summary>
			/// Deletion mode.  Clicking an item slot twice will delete the item in it.
			/// </summary>
			Deletion,
			/// <summary>
			/// Selling mode.  Clicking a non-selected item slot or right clicking a selected slot will open a quantity popup.  Contains additional buttons for handling selling items.
			/// </summary>
			Selling,
			/// <summary>
			/// Information mode.  Hovering over certain areas of the UI will display information about them.
			/// </summary>
			Info
		}

		internal static int slotFocus = -1;

		private static int rightClickTimer;
		private static int maxRightClickTimer = startMaxRightClickTimer;

		internal static ActionMode currentMode = ActionMode.Normal;
		internal static int actionSlotFocus = -1;

		private static bool ForciblySeparateItemStacks => currentMode is ActionMode.Deletion;

		internal static void ResetSlotFocus()
		{
			slotFocus = -1;
			rightClickTimer = 0;
			maxRightClickTimer = startMaxRightClickTimer;
		}

		internal static void SlotFocusLogic()
		{
			if (MagicUI.CurrentlyRefreshing)
				return;  // Delay logic until threading stops

			if (slotFocus == -1 || slotFocus >= items.Count || !Main.mouseItem.IsAir && (!StorageAggregator.CanCombineItems(Main.mouseItem, items[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
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

					MagicUI.SetRefresh();
					SoundEngine.PlaySound(SoundID.MenuTick);
				}

				rightClickTimer--;
			}
		}

		internal static void SetActiveModeWithForcedJump(ActionMode mode, bool activate) {
			if (activate) {
				currentMode = mode;
				MagicUI.storageUI.SetPage(MagicUI.storageUI.DefaultPage);
			} else if (currentMode == mode)
				currentMode = ActionMode.Normal;
		}
	}
}
