using MagicStorage.CrossMod;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.Common.Systems {
	internal static class ItemStackSplitting {
		// This class wraps around the fields used by vanilla to handle the super-fast stack splitting

		public static bool CanSplitOntoMouse(Item target)
			=> Main.mouseItem.IsAir || (Main.mouseItem.stack < Main.mouseItem.maxStack && StorageAggregator.CanCombineItems(Main.mouseItem, target));

		public static bool TickOneSplitOntoMouse(Item target, Func<Item, int, Item> withdrawFunc, out bool waitingForNextSplit, Action<Item> onWithdrawn = null) {
			if (!CanSplitOntoMouse(target)) {
				waitingForNextSplit = false;
				return false;
			}

			if (Main.stackSplit > 1) {
				waitingForNextSplit = true;
				return false;
			}

			Item result = withdrawFunc(target, int.Min(target.maxStack, Main.superFastStack + 1));
			if (result is null || result.IsAir) {
				waitingForNextSplit = false;
				return false;
			}

			if (Main.mouseItem.IsAir) {
				// Simply set the mouse item
				Main.mouseItem = result;
			} else {
				// Stack onto the mouse
				Utility.CallOnStackHooks(Main.mouseItem, result, result.stack);
				Main.mouseItem.stack += result.stack;
			}

			onWithdrawn?.Invoke(result);

			SoundEngine.PlaySound(SoundID.MenuTick);

			// Reset the variables
			ItemSlot.RefreshStackSplitCooldown();

			waitingForNextSplit = false;
			return true;
		}
	}
}
