using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	public class NewUIButtonChoice : UIElement {
		private readonly Action _onChanged;

		public int Choice { get; set; }

		private readonly List<ChoiceElement> choices = new();

		private readonly int buttonSize, buttonPadding;

		public NewUIButtonChoice(Action onChanged, int buttonSize, int buttonPadding = 1) {
			ArgumentNullException.ThrowIfNull(onChanged);

			_onChanged = onChanged;
			this.buttonSize = buttonSize;
			this.buttonPadding = buttonPadding;

			Height.Set(buttonSize, 0f);
			MinHeight.Set(buttonSize, 0f);

			SetPadding(0);
		}

		public void AssignButtons(Asset<Texture2D>[] textures, LocalizedText[] texts) {
			if (textures.Length != texts.Length || textures.Length == 0)
				throw new ArgumentException("Array Lengths must match and be non-zero");

			int width = (buttonSize + buttonPadding) * textures.Length - buttonPadding;

			Width.Set(width, 0f);
			MinWidth.Set(width, 0f);

			foreach (var choice in choices)
				choice.Remove();

			choices.Clear();

			int left = 0;

			for (int i = 0; i < textures.Length; i++) {
				var asset = textures[i];
				var text = texts[i];

				ChoiceElement choice = new(i, asset, text, buttonSize);
				choice.Left.Set(left, 0f);

				left += buttonSize + buttonPadding;

				choices.Add(choice);
				Append(choice);
			}

			//Always reset the choice to the first option
			Choice = 0;
		}

		private class ChoiceElement : UIElement {
			private static Asset<Texture2D> BackTexture => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
			private static Asset<Texture2D> BackTextureActive => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);

			public readonly Asset<Texture2D> texture;
			public readonly LocalizedText text;

			private bool hovering;

			public readonly int option;
			public readonly int buttonSize;

			public ChoiceElement(int option, Asset<Texture2D> texture, LocalizedText text, int buttonSize = 21) {
				this.option = option;
				this.texture = texture;
				this.text = text;
				this.buttonSize = buttonSize;
			}

			public override void MouseOver(UIMouseEvent evt) {
				base.MouseOver(evt);

				hovering = true;

				MagicUI.mouseText = text.Value;
			}

			public override void MouseOut(UIMouseEvent evt) {
				base.MouseOut(evt);

				hovering = false;
				MagicUI.mouseText = "";
			}

			public override void Click(UIMouseEvent evt) {
				base.Click(evt);

				if (Parent is NewUIButtonChoice buttons) {
					int old = buttons.Choice;
					buttons.Choice = option;

					if (old != buttons.Choice)
						buttons._onChanged();
				}
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				if (Parent is not NewUIButtonChoice buttons)
					return;

				CalculatedStyle dim = GetDimensions();
				Asset<Texture2D> background = option == buttons.Choice ? BackTextureActive : BackTexture;
				Vector2 drawPos = new(dim.X, dim.Y);
				Color color = hovering ? Color.Silver : Color.White;
				Main.spriteBatch.Draw(background.Value, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
				Main.spriteBatch.Draw(texture.Value, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
			}
		}
	}
}
