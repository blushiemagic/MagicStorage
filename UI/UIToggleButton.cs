using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI
{
	public class UIToggleButton : UIElement
	{
		private readonly Texture2D _button;
		private readonly LocalizedText _name;
		private readonly int buttonSize;
		private readonly Action onChanged;

		public UIToggleButton(Action onChanged, Texture2D button, LocalizedText name, int buttonSize = 21)
		{
			this.buttonSize = buttonSize;
			this.onChanged = onChanged;
			_button = button;
			_name = name;
			Width.Set(buttonSize, 0f);
			MinWidth.Set(buttonSize, 0f);
			Height.Set(buttonSize, 0f);
			MinHeight.Set(buttonSize, 0f);
		}

		public bool Value { get; set; }

		public override void Update(GameTime gameTime)
		{
			bool oldValue = Value;
			if (StorageGUI.MouseClicked && Parent != null)
			{
				if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y))
				{
					Value = !Value;
				}
			}

			if (oldValue != Value)
			{
				onChanged?.Invoke();
			}
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
			Texture2D backTexture = MagicStorage.Instance.GetTexture("Assets/SortButtonBackground");
			Texture2D backTextureActive = MagicStorage.Instance.GetTexture("Assets/SortButtonBackgroundActive");
			CalculatedStyle dim = GetDimensions();
			Texture2D texture = Value ? backTextureActive : backTexture;
			var drawPos = new Vector2(dim.X, dim.Y);
			Color color = MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y) ? Color.Silver : Color.White;
			Main.spriteBatch.Draw(texture, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
			Main.spriteBatch.Draw(_button, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
		}

		public void DrawText()
		{
			if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y))
			{
				Main.instance.MouseText(_name.Value);
			}
		}
	}
}
