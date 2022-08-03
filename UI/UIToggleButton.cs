using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI
{
	public class UIToggleButton : UIElement
	{
		private static readonly Asset<Texture2D> BackTexture = MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
		private static readonly Asset<Texture2D> BackTextureActive = MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);
		private readonly Asset<Texture2D> button;
		private readonly LocalizedText name;
		private readonly int buttonSize;
		private readonly Action onChanged;

		public bool Value { get; set; }

		public UIToggleButton(Action onChanged, Asset<Texture2D> button, LocalizedText name, int buttonSize = 21)
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

		public override void Update(GameTime gameTime)
		{
			bool oldValue = Value;
			if (StorageGUI.MouseClicked && Parent is not null)
				if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y))
					Value = !Value;

			if (oldValue != Value)
				onChanged?.Invoke();
		}

		private bool MouseOverButton(int mouseX, int mouseY)
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(this);
			float left = dim.X;
			float right = left + buttonSize * Main.UIScale;
			float top = dim.Y;
			float bottom = top + buttonSize * Main.UIScale;
			return mouseX > left && mouseX < right && mouseY > top && mouseY < bottom;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dim = GetDimensions();
			Asset<Texture2D> texture = Value ? BackTextureActive : BackTexture;
			Vector2 drawPos = new(dim.X, dim.Y);
			Color color = MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y) ? Color.Silver : Color.White;
			Main.spriteBatch.Draw(texture.Value, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
			Main.spriteBatch.Draw(button.Value, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
		}

		public void DrawText()
		{
			if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y))
				Main.instance.MouseText(name.Value);
		}
	}
}
