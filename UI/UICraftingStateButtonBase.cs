using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class UICraftingStateButtonBase : UITextPanel<LocalizedText> {
		internal bool hovering;

		public UICraftingStateButtonBase(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			hovering = true;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			hovering = false;
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (CraftingGUI.IsAvailable(CraftingGUI.selectedRecipe, false) && CraftingGUI.PassesBlock(CraftingGUI.selectedRecipe)) {
				if (hovering) {
					OnHoveringAndValidRecipe(gameTime);
					BackgroundColor = new Color(73, 94, 171);
				} else
					BackgroundColor = new Color(63, 82, 151) * 0.7f;
			} else
				BackgroundColor = new Color(30, 40, 100) * 0.7f;
		}

		protected virtual void OnHoveringAndValidRecipe(GameTime gameTime) { }
	}
}
