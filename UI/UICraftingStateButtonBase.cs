using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;

namespace MagicStorage.UI {
	public abstract class UICraftingStateButtonBase : UITextPanel<LocalizedText> {
		public UICraftingStateButtonBase(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (MagicUI.CurrentlyRefreshing)
				return;  // Do not read anything until refreshing is completed

			if (IsValid()) {
				if (IsMouseHovering) {
					OnHoveringAndValidRecipe(gameTime);
					BackgroundColor = new Color(73, 94, 171);
				} else
					BackgroundColor = new Color(63, 82, 151) * 0.7f;
			} else
				BackgroundColor = new Color(30, 40, 100) * 0.7f;
		}

		private static bool IsValid() {
			if (MagicUI.IsCraftingUIOpen())
				return CraftingGUI.IsCurrentRecipeFullyAvailable();
			else if (MagicUI.IsDecraftingUIOpen())
				return DecraftingGUI.selectedItem > ItemID.None && DecraftingGUI.IsAvailable(DecraftingGUI.selectedItem);
			return false;
		}

		protected abstract void OnHoveringAndValidRecipe(GameTime gameTime);
	}
}
