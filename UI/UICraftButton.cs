using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class UICraftButton : UICraftingStateButtonBase {
		public UICraftButton(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			MagicUI.mouseText = "";
		}

		protected override void OnHoveringAndValidRecipe(GameTime gameTime) {
			if (CraftingGUI.selectedRecipe is not null)
				MagicUI.mouseText = Language.GetText("Mods.MagicStorage.CraftTooltip" + (MagicStorageConfig.UseOldCraftMenu ? "Old" : "")).Value;

			if (StorageGUI.curMouse.LeftButton == ButtonState.Pressed) {
				if (StorageGUI.oldMouse.LeftButton == ButtonState.Released)
					SoundEngine.PlaySound(SoundID.MenuTick);

				LeftMouseDown(new(this, UserInterface.ActiveInstance.MousePosition));

				bool stillCrafting = false;
				CraftingGUI.ClickCraftButton(ref stillCrafting);
			} else {
				CraftingGUI.craftTimer = 0;
				CraftingGUI.maxCraftTimer = CraftingGUI.StartMaxCraftTimer;
			}
		}
	}
}
