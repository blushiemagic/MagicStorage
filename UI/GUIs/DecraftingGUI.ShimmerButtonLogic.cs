using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework.Input;
using Terraria.Audio;
using Terraria.ID;
using Terraria;

namespace MagicStorage {
	partial class DecraftingGUI {
		internal static void ClickShimmerButton(ref bool stillCrafting) {
			if (CraftingGUI.craftTimer <= 0)
			{
				CraftingGUI.craftTimer = CraftingGUI.maxCraftTimer;
				CraftingGUI.maxCraftTimer = CraftingGUI.maxCraftTimer * 3 / 4;
				if (CraftingGUI.maxCraftTimer <= 0)
					CraftingGUI.maxCraftTimer = 1;

				int amount = CraftingGUI.craftAmountTarget;

				if (MagicStorageConfig.UseOldCraftMenu && Main.keyState.IsKeyDown(Keys.LeftControl))
					amount = 9999;

				Shimmer(amount);

				SetNextDefaultItemCollectionToRefresh(selectedItem);
				MagicUI.SetRefresh();

				var sound = Main.rand.Next(4) switch {
					0 => SoundID.Shimmer1,
					1 => SoundID.Shimmer2,
					2 => SoundID.ShimmerWeak1,
					_ => SoundID.ShimmerWeak2
				};

				SoundEngine.PlaySound(sound);  // Shimmer splash
				SoundEngine.PlaySound(SoundID.Item176);  // Shimmer bubbling
			}

			CraftingGUI.craftTimer--;
			stillCrafting = true;
		}

		internal static void ClickAmountButton(int amount, bool offset) {
			if (MagicUI.CurrentlyRefreshing)
				return;  // Do not read anything until refreshing is completed

			if (offset && (amount == 1 || CraftingGUI.craftAmountTarget > 1))
				CraftingGUI.craftAmountTarget += amount;
			else
				CraftingGUI.craftAmountTarget = amount;  //Snap directly to the amount if the amount target was 1 (this makes clicking 10 when at 1 just go to 10 instead of 11)

			ClampShimmerAmount();

			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		internal static void ClampShimmerAmount() {
			if (MagicUI.CurrentlyRefreshing)
				return;  // Recipe/ingredient information may not be available

			if (CraftingGUI.craftAmountTarget < 1 || selectedItem == -1 || !IsAvailable(selectedItem))
				CraftingGUI.craftAmountTarget = 1;
			else {
				int max = CraftingGUI.itemCounts.TryGetValue(selectedItem, out int count) ? count : 1;

				if (CraftingGUI.craftAmountTarget > max)
					CraftingGUI.craftAmountTarget = max;
			}
		}
	}
}
