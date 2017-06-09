using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	public class UIButtonChoice : UIElement
	{
		private const int buttonSize = 32;
		private const int buttonPadding = 8;

		private Texture2D[] buttons;
		private LocalizedText[] names;
		private int choice = 0;

		public int Choice
		{
			get
			{
				return choice;
			}
		}

		public UIButtonChoice(Texture2D[] buttons, LocalizedText[] names)
		{
			if (buttons.Length != names.Length || buttons.Length == 0)
			{
				throw new ArgumentException();
			}
			this.buttons = buttons;
			this.names = names;
			int width = buttonSize * buttons.Length + buttonPadding * (buttons.Length - 1);
			this.Width.Set(width, 0f);
			this.MinWidth.Set(width, 0f);
			this.Height.Set(buttonSize, 0f);
			this.MinHeight.Set(buttonSize, 0f);
		}

		public override void Update(GameTime gameTime)
		{
			int oldChoice = choice;
			if (StorageGUI.MouseClicked && Parent != null)
			{
				for (int k = 0; k < buttons.Length; k++)
				{
					if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y, k))
					{
						choice = k;
						break;
					}
				}
			}
			if (oldChoice != choice)
			{
				StorageGUI.RefreshItems();
			}
		}

		private bool MouseOverButton(int mouseX, int mouseY, int button)
		{
			CalculatedStyle dim = GetDimensions();
			float left = dim.X + button * (buttonSize + buttonPadding);
			float right = left + buttonSize;
			float top = dim.Y;
			float bottom = top + buttonSize;
			return mouseX > left && mouseX < right && mouseY > top && mouseY < bottom;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Texture2D backTexture = MagicStorage.Instance.GetTexture("SortButtonBackground");
			Texture2D backTextureActive = MagicStorage.Instance.GetTexture("SortButtonBackgroundActive");
			CalculatedStyle dim = GetDimensions();
			for (int k = 0; k < buttons.Length; k++)
			{
				Texture2D texture = k == choice ? backTextureActive : backTexture;
				Vector2 drawPos = new Vector2(dim.X + k * (buttonSize + buttonPadding), dim.Y);
				Color color = MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y, k) ? Color.Silver : Color.White;
				Main.spriteBatch.Draw(texture, drawPos, color);
				Main.spriteBatch.Draw(buttons[k], drawPos + new Vector2(1f), Color.White);
			}
		}

		public void DrawText()
		{
			for (int k = 0; k < buttons.Length; k++)
			{
				if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y, k))
				{
					Main.instance.MouseText(names[k].Value);
				}
			}
		}
	}
}