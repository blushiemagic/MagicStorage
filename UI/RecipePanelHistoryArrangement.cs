using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class RecipePanelHistoryArrangement : UIElement {
		public class HistoryAdjustmentButton : UITextPanel<char> {
			public readonly LocalizedText localization;
			public readonly bool forward;

			public readonly RecipeHistory history;

			public HistoryAdjustmentButton(RecipeHistory history, bool forward, string hoverKey, float scale = 1f) : base(forward ? '>' : '<', scale) {
				this.history = history;
				this.forward = forward;
				localization = Language.GetText(hoverKey);
			}

			public override void Click(UIMouseEvent evt) {
				base.Click(evt);

				int offset = forward ? 1 : -1;

				history.Goto(history.Current + offset);
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOver(UIMouseEvent evt) {
				base.MouseOver(evt);

				TextColor = Color.Yellow;
				MagicUI.mouseText = localization.Value;
			}

			public override void MouseOut(UIMouseEvent evt) {
				base.MouseOut(evt);

				TextColor = Color.White;
				MagicUI.mouseText = "";
			}
		}

		public readonly RecipeHistory history;

		public RecipeHistoryButton button;
		public HistoryAdjustmentButton prev, next;

		public event Action OnButtonClicked;

		public RecipePanelHistoryArrangement(RecipeHistory history, float scale) {
			this.history = history;

			button = new(scale);

			button.OnClick += (evt, e) => OnButtonClicked?.Invoke();

			prev = new(history, false, "Mods.MagicStorage.PrevHistory", scale) {
				VAlign = 0.5f
			};

			next = new(history, true, "Mods.MagicStorage.NextHistory", scale) {
				VAlign = 0.5f
			};

			SetPadding(0);
		}

		public override void OnInitialize() {
			next.Left.Set(prev.MinWidth.Pixels + 2, 0f);
			button.Left.Set(next.Left.Pixels + next.MinWidth.Pixels + 2, 0f);

			Width.Set(button.Left.Pixels + button.Width.Pixels, 0f);
			Height = button.Height;

			Append(button);
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (history.Current > 0 && prev.Parent is null)
				Append(prev);
			else if (history.Current <= 0 && prev.Parent is not null)
				prev.Remove();

			if (history.Current < history.Count - 1 && next.Parent is null)
				Append(next);
			else if (history.Current >= history.Count - 1 && next.Parent is not null)
				next.Remove();
		}
	}
}
