using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI
{
	public class UIButtonChoice : UIElement
	{
		private readonly Action _onChanged;
		private readonly int buttonPadding;

		private readonly Asset<Texture2D>[] buttons;
		private readonly int buttonSize;

		private readonly LocalizedText[] names;
		private static readonly Asset<Texture2D> backTexture = ModContent.Request<Texture2D>("Assets/SortButtonBackground");
		private static readonly Asset<Texture2D> backTextureActive = ModContent.Request<Texture2D>("Assets/SortButtonBackgroundActive");

		public UIButtonChoice(Action onChanged, Asset<Texture2D>[] buttons, LocalizedText[] names, int buttonSize = 21, int buttonPadding = 1)
		{
			if (buttons.Length != names.Length || buttons.Length == 0)
				throw new ArgumentException();

			_onChanged = onChanged;
			this.buttonSize = buttonSize;
			this.buttonPadding = buttonPadding;
			this.buttons = buttons;
			this.names = names;
			int width = buttonSize * buttons.Length + buttonPadding * (buttons.Length - 1);
			Width.Set(width, 0f);
			MinWidth.Set(width, 0f);
			Height.Set(buttonSize, 0f);
			MinHeight.Set(buttonSize, 0f);
		}

		public int Choice { get; set; }

		public override void Update(GameTime gameTime)
		{
			int oldChoice = Choice;
			if (StorageGUI.MouseClicked && Parent != null)
				for (int k = 0; k < buttons.Length; k++)
					if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y, k))
					{
						Choice = k;
						break;
					}

			if (oldChoice != Choice)
				_onChanged?.Invoke();
		}

		private bool MouseOverButton(int mouseX, int mouseY, int button)
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(this);
			float left = dim.X + button * (buttonSize + buttonPadding) * Main.UIScale;
			float right = left + buttonSize * Main.UIScale;
			float top = dim.Y;
			float bottom = top + buttonSize * Main.UIScale;
			return mouseX > left && mouseX < right && mouseY > top && mouseY < bottom;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dim = GetDimensions();
			for (int k = 0; k < buttons.Length; k++)
			{
				Asset<Texture2D> texture = k == Choice ? backTextureActive : backTexture;
				Vector2 drawPos = new(dim.X + k * (buttonSize + buttonPadding), dim.Y);
				Color color = MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y, k) ? Color.Silver : Color.White;
				Main.spriteBatch.Draw(texture.Value, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
				Main.spriteBatch.Draw(buttons[k].Value, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
			}
		}

		public void DrawText()
		{
			for (int k = 0; k < buttons.Length; k++)
				if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y, k))
					Main.instance.MouseText(names[k].Value);
		}
	}
}
