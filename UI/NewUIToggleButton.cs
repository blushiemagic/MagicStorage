using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	public class NewUIToggleButton : UIElement {
		private static readonly Asset<Texture2D> BackTexture = MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
		private static readonly Asset<Texture2D> BackTextureActive = MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);
		private readonly Asset<Texture2D> button;
		private readonly LocalizedText name;
		private readonly int buttonSize;
		private readonly Action onChanged;

		public bool Value { get; set; }

		private bool hovering;

		public NewUIToggleButton(Action onChanged, Asset<Texture2D> button, LocalizedText name, int buttonSize)
		{
			this.buttonSize = buttonSize;
			this.onChanged = onChanged;
			this.button = button;
			this.name = name;
			Width.Set(buttonSize, 0f);
			MinWidth.Set(buttonSize, 0f);
			Height.Set(buttonSize, 0f);
			MinHeight.Set(buttonSize, 0f);
		}

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);

			bool oldValue = Value;
			Value = !Value;

			if (oldValue != Value)
				onChanged?.Invoke();
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			hovering = true;
			MagicUI.mouseText = name.Value;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			hovering = false;
			MagicUI.mouseText = "";
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dim = GetDimensions();
			Asset<Texture2D> texture = Value ? BackTextureActive : BackTexture;
			Vector2 drawPos = new(dim.X, dim.Y);
			Color color = hovering ? Color.Silver : Color.White;
			spriteBatch.Draw(texture.Value, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
			spriteBatch.Draw(button.Value, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
		}
	}
}
