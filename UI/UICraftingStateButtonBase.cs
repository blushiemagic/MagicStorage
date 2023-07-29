﻿using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace MagicStorage.UI {
	internal class UICraftingStateButtonBase : UITextPanel<LocalizedText> {
		public UICraftingStateButtonBase(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (StorageGUI.CurrentlyRefreshing)
				return;  // Do not read anything until refreshing is completed

			// TODO: cache this calculation?
			if (CraftingGUI.IsCurrentRecipeFullyAvailable()) {
				if (IsMouseHovering) {
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
