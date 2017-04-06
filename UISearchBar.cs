using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;

namespace MagicStorage
{
	public class UISearchBar : UIElement
	{
		private const int padding = 4;
		private string text = string.Empty;
		private int cursorPosition = 0;
		private bool hasFocus = false;
		private int cursorTimer = 0;

		public UISearchBar()
		{
			this.SetPadding(padding);
		}

		public string Text
		{
			get
			{
				return text;
			}
		}

		public void Reset()
		{
			text = string.Empty;
			cursorPosition = 0;
			hasFocus = false;
		}

		public override void Update(GameTime gameTime)
		{
			cursorTimer++;
			cursorTimer %= 60;

			if (StorageGUI.MouseClicked && Parent != null)
			{
				CalculatedStyle dim = GetDimensions();
				MouseState mouse = StorageGUI.curMouse;
				bool mouseOver = mouse.X > dim.X && mouse.X < dim.X + dim.Width && mouse.Y > dim.Y && mouse.Y < dim.Y + dim.Height;
				if (!hasFocus && mouseOver)
				{
					hasFocus = true;
					Main.blockInput = true;
				}
				else if (hasFocus && !mouseOver)
				{
					hasFocus = false;
					Main.blockInput = false;
					cursorPosition = text.Length;
				}
			}

			if (hasFocus)
			{
				for (int k = (int)Keys.A; k <= (int)Keys.Z; k++)
				{
					if (KeyTyped((Keys)k))
					{
						InsertKey((char)(k - (int)Keys.A + 'a'));
					}
				}
				for (int k = (int)Keys.D0; k <= (int)Keys.D9; k++)
				{
					if (KeyTyped((Keys)k))
					{
						InsertKey((char)(k - (int)Keys.D0 + '0'));
					}
				}
				foreach (Keys key in keyMap.Keys)
				{
					if (KeyTyped(key))
					{
						InsertKey(keyMap[key]);
					}
				}
			}
			base.Update(gameTime);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Texture2D texture = ModLoader.GetTexture("MagicStorage/SearchBar");
			CalculatedStyle dim = GetDimensions();
			int innerWidth = (int)dim.Width - 2 * padding;
			int innerHeight = (int)dim.Height - 2 * padding;
			spriteBatch.Draw(texture, dim.Position(), new Rectangle(0, 0, padding, padding), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int)dim.X + padding, (int)dim.Y, innerWidth, padding), new Rectangle(padding, 0, 1, padding), Color.White);
			spriteBatch.Draw(texture, new Vector2(dim.X + padding + innerWidth, dim.Y), new Rectangle(padding + 1, 0, padding, padding), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int)dim.X, (int)dim.Y + padding, padding, innerHeight), new Rectangle(0, padding, padding, 1), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int)dim.X + padding, (int)dim.Y + padding, innerWidth, innerHeight), new Rectangle(padding, padding, 1, 1), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int)dim.X + padding + innerWidth, (int)dim.Y + padding, padding, innerHeight), new Rectangle(padding + 1, padding, padding, 1), Color.White);
			spriteBatch.Draw(texture, new Vector2(dim.X, dim.Y + padding + innerHeight), new Rectangle(0, padding + 1, padding, padding), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int)dim.X + padding, (int)dim.Y + padding + innerHeight, innerWidth, padding), new Rectangle(padding, padding + 1, 1, padding), Color.White);
			spriteBatch.Draw(texture, new Vector2(dim.X + padding + innerWidth, dim.Y + padding + innerHeight), new Rectangle(padding + 1, padding + 1, padding, padding), Color.White);

			bool isEmpty = text.Length == 0;
			string drawText = isEmpty ? "Search" : text;
			SpriteFont font = Main.fontMouseText;
			Vector2 size = font.MeasureString(drawText);
			float scale = innerHeight / size.Y;
			if (isEmpty && hasFocus)
			{
				drawText = string.Empty;
				isEmpty = false;
			}
			Color color = Color.Black;
			if (isEmpty)
			{
				color *= 0.75f;
			}
			spriteBatch.DrawString(font, drawText, new Vector2(dim.X + padding, dim.Y + padding), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			if (!isEmpty && hasFocus && cursorTimer < 30)
			{
				float drawCursor = font.MeasureString(drawText.Substring(0, cursorPosition)).X * scale;
				spriteBatch.DrawString(font, "|", new Vector2(dim.X + padding + drawCursor, dim.Y + padding), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
		}

		public bool KeyTyped(Keys key)
		{
			return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
		}

		private void InsertKey(char letter)
		{
			if ((letter >= 'a' && letter <= 'z') || (letter >= '0' && letter <= '9') || letter == ' ' || letter == '-' || letter == '.')
			{
				text = text.Substring(0, cursorPosition) + letter + text.Substring(cursorPosition);
				cursorPosition++;
				StorageGUI.RefreshItems();
			}
			else if (letter == '\b' && cursorPosition > 0)
			{
				text = text.Substring(0, cursorPosition - 1) + text.Substring(cursorPosition);
				cursorPosition--;
				StorageGUI.RefreshItems();
			}
			else if (letter == '<' && cursorPosition > 0)
			{
				cursorPosition--;
			}
			else if (letter == '>' && cursorPosition < text.Length)
			{
				cursorPosition++;
			}
			else if (letter == '\n' || letter == '\t')
			{
				hasFocus = false;
				Main.blockInput = false;
			}
			cursorTimer = 0;
		}

		private static Dictionary<Keys, char> keyMap = new Dictionary<Keys, char>()
		{
			{ Keys.Space, ' ' },
			{ Keys.OemMinus, '-' },
			{ Keys.OemPeriod, '.' },
			{ Keys.Back, '\b' },
			{ Keys.Left, '<' },
			{ Keys.Right, '>' },
			{ Keys.Enter, '\n' },
			{ Keys.Escape, '\n' },
			{ Keys.Tab, '\t' }
		};
	}
}