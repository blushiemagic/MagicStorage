﻿using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UICraftAmountAdjustment : UICraftingStateButtonBase {
		public int Amount { get; private set; }

		public bool AmountIsOffset { get; private set; }

		public UICraftAmountAdjustment(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

		public void SetAmountInformation(int amount, bool amountIsOffset) {
			Amount = amount;
			AmountIsOffset = amountIsOffset;
		}

		protected override void OnHoveringAndValidRecipe(GameTime gameTime) {
			if (StorageGUI.MouseClicked) {
				SoundEngine.PlaySound(SoundID.MenuTick);

				LeftClick(new(this, Main.MouseScreen));

				if (MagicUI.IsCraftingUIOpen())
					CraftingGUI.ClickAmountButton(Amount, AmountIsOffset);
				else if (MagicUI.IsDecraftingUIOpen())
					DecraftingGUI.ClickAmountButton(Amount, AmountIsOffset);
			}
		}
	}
}
