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
	    readonly Action _onChanged;
	    private int buttonSize;
		private int buttonPadding;

		private Texture2D[] buttons;
		private LocalizedText[] names;
        
	    private int choice = 0;
        
		public int Choice
		{
			get
			{
				return choice;
			}
		    set
		    {
		        choice = value;
		    }
		}

		public UIButtonChoice(Action onChanged, Texture2D[] buttons, LocalizedText[] names, int buttonSize = 21, int buttonPadding = 1)
		{
			if (buttons.Length != names.Length || buttons.Length == 0)
			{
				throw new ArgumentException();
			}

		    _onChanged = onChanged;
		    this.buttonSize = buttonSize;
            this.buttonPadding = buttonPadding;
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
                _onChanged?.Invoke();
			}
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
			Texture2D backTexture = MagicStorage.Instance.GetTexture("SortButtonBackground");
			Texture2D backTextureActive = MagicStorage.Instance.GetTexture("SortButtonBackgroundActive");
			CalculatedStyle dim = GetDimensions();
			for (int k = 0; k < buttons.Length; k++)
			{
				Texture2D texture = k == choice ? backTextureActive : backTexture;
				Vector2 drawPos = new Vector2(dim.X + k * (buttonSize + buttonPadding), dim.Y);
				Color color = MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y, k) ? Color.Silver : Color.White;
				Main.spriteBatch.Draw(texture, new Rectangle((int)drawPos.X,(int)drawPos.Y,buttonSize,buttonSize), color);
			    Main.spriteBatch.Draw(buttons[k], new Rectangle((int)drawPos.X + 1, (int)drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
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