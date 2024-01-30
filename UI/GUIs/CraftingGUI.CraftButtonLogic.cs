using MagicStorage.Common;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using MagicStorage.Common.Systems;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static bool clampCraftAmountAllowCacheReset;

		public static int craftAmountTarget;

		internal static int craftTimer;
		internal static int maxCraftTimer = StartMaxCraftTimer;

		internal static void ClickCraftButton(ref bool stillCrafting) {
			if (craftTimer <= 0)
			{
				craftTimer = maxCraftTimer;
				maxCraftTimer = maxCraftTimer * 3 / 4;
				if (maxCraftTimer <= 0)
					maxCraftTimer = 1;

				int amount = craftAmountTarget;

				if (MagicStorageConfig.UseOldCraftMenu && Main.keyState.IsKeyDown(Keys.LeftControl))
					amount = 9999;

				Craft(amount);

				IEnumerable<int> allItemTypes = selectedRecipe.requiredItem.Select(i => i.type).Prepend(selectedRecipe.createItem.type);

				//If no recipes were affected, that's fine, none of the recipes will be touched due to the calculated Recipe array being empty
				SetNextDefaultRecipeCollectionToRefresh(allItemTypes);
				MagicUI.SetRefresh();
				SoundEngine.PlaySound(SoundID.Grab);
			}

			craftTimer--;
			stillCrafting = true;
		}

		internal static void ClickAmountButton(int amount, bool offset) {
			if (MagicUI.CurrentlyRefreshing)
				return;  // Do not read anything until refreshing is completed

			int oldTarget = craftAmountTarget;
			if (offset && (amount == 1 || craftAmountTarget > 1))
				craftAmountTarget += amount;
			else
				craftAmountTarget = amount;  //Snap directly to the amount if the amount target was 1 (this makes clicking 10 when at 1 just go to 10 instead of 11)

			using (FlagSwitch.ToggleFalse(ref clampCraftAmountAllowCacheReset))
				ClampCraftAmount();

			if (craftAmountTarget != oldTarget) {
				ResetCachedBlockedIngredientsCheck();
				ResetCachedCraftingSimulation();

				if (MagicStorageConfig.IsRecursionEnabled) {
					RefreshStorageItems();
					MagicUI.SetRefresh();
				}
			}

			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		internal static void ClampCraftAmount() {
			if (MagicUI.CurrentlyRefreshing)
				return;  // Recipe/ingredient information may not be available

			int oldTarget = craftAmountTarget;

			if (craftAmountTarget < 1 || selectedRecipe is null || selectedRecipe.createItem.maxStack == 1 || !IsCurrentRecipeFullyAvailable())
				craftAmountTarget = 1;
			else {
				int amountCraftable = AmountCraftableForCurrentRecipe();
				int max = Utils.Clamp(amountCraftable, 1, selectedRecipe.createItem.maxStack);

				if (craftAmountTarget > max)
					craftAmountTarget = max;
			}

			if (clampCraftAmountAllowCacheReset && oldTarget != craftAmountTarget) {
				ResetCachedBlockedIngredientsCheck();
				ResetCachedCraftingSimulation();

				if (MagicStorageConfig.IsRecursionEnabled) {
					RefreshStorageItems();
					MagicUI.SetRefresh();
				}
			}
		}
	}
}
