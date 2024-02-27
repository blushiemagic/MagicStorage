using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UICraftButton : UICraftingStateButtonBase {
		private readonly string _hoverTextKey;

		public UICraftButton(LocalizedText text, string hoverTextKey) : base(text) {
			_hoverTextKey = "Mods.MagicStorage." + hoverTextKey;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			MagicUI.mouseText = "";
		}

		protected override void OnHoveringAndValidRecipe(GameTime gameTime) {
			if (DisplayHoverText()) {
				string key = _hoverTextKey;
				if (MagicStorageConfig.UseOldCraftMenu)
					key += "Old";

				MagicUI.mouseText = Language.GetTextValue(key);
			}

			if (StorageGUI.curMouse.LeftButton == ButtonState.Pressed) {
				if (StorageGUI.oldMouse.LeftButton == ButtonState.Released)
					SoundEngine.PlaySound(SoundID.MenuTick);

				LeftMouseDown(new(this, Main.MouseScreen));

				bool stillCrafting = false;
				HandleCraft(ref stillCrafting);
			} else {
				CraftingGUI.craftTimer = 0;
				CraftingGUI.maxCraftTimer = CraftingGUI.StartMaxCraftTimer;
			}
		}

		protected virtual bool DisplayHoverText() => CraftingGUI.selectedRecipe is not null;

		protected virtual void HandleCraft(ref bool stillCrafting) => CraftingGUI.ClickCraftButton(ref stillCrafting);
	}
}
