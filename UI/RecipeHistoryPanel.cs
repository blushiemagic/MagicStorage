using MagicStorage.Common;
using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;

namespace MagicStorage.UI {
	internal class RecipeHistoryPanel : UIDragablePanel {
		public readonly RecipeHistory history;

		public RecipeHistoryPanel(bool stopItemUse, RecipeHistory history, IEnumerable<(string key, LocalizedText text)> menuOptions) : base(stopItemUse, menuOptions) {
			this.history = history;

			// Same color as vanilla, but without the transparency
			BackgroundColor = new Color(63, 82, 151);

			viewArea.SetPadding(0);

			viewArea.Append(history.list);

			var clearButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.ClearHistory"));
			clearButton.SetPadding(7);
			clearButton.Width.Set(100, 0);
			clearButton.Left.Set(-45 - 100, 1);
			clearButton.BackgroundColor.A = 255;
			clearButton.OnLeftClick += (evt, element) => {
				SoundEngine.PlaySound(SoundID.MenuTick);
				this.history.Clear();

				if (CraftingGUI.selectedRecipe is not null) {
					this.history.AddHistory(CraftingGUI.selectedRecipe);
					this.history.RefreshEntries();
				}

				this.history.scroll.ViewPosition = 0f;
			};
			header.Append(clearButton);
		}

		public override void Update(GameTime gameTime) {
			using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, IsMouseHovering)) {
				//Prevent moving the panel
				Dragging = false;

				base.Update(gameTime);
			}
		}

		public override void Draw(SpriteBatch spriteBatch) {
			using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, IsMouseHovering)) {
				base.Draw(spriteBatch);
			}
		}
	}
}
