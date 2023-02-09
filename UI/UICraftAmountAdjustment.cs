using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class UICraftAmountAdjustment : UICraftingStateButtonBase {
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

				#if TML_144
				LeftClick(new(this, UserInterface.ActiveInstance.MousePosition));
				#else
				Click(new(this, UserInterface.ActiveInstance.MousePosition));
				#endif
				CraftingGUI.ClickAmountButton(Amount, AmountIsOffset);
			}
		}
	}
}
