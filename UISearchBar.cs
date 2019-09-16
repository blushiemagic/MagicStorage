using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	public class UISearchBar : UIElement
	{
		private static List<UISearchBar> searchBars = new List<UISearchBar>();

		private const int padding = 4;
		private LocalizedText defaultText = Language.GetText("Mods.MagicStorage.Search");
		private string text = string.Empty;
		private int cursorPosition = 0;
		private bool hasFocus = false;
		private int cursorTimer = 0;

		public UISearchBar()
		{
			this.SetPadding(padding);
			searchBars.Add(this);
		}

		public UISearchBar(LocalizedText defaultText) : this()
		{
			this.defaultText = defaultText;
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
			CheckBlockInput();
		}

		public override void Update(GameTime gameTime)
		{
			cursorTimer++;
			cursorTimer %= 60;

			Rectangle dim = InterfaceHelper.GetFullRectangle(this);
			MouseState mouse = StorageGUI.curMouse;
			bool mouseOver = mouse.X > dim.X && mouse.X < dim.X + dim.Width && mouse.Y > dim.Y && mouse.Y < dim.Y + dim.Height;

			if (StorageGUI.MouseClicked && Parent != null)
			{
				if (!hasFocus && mouseOver)
				{
					hasFocus = true;
					CheckBlockInput();
				}
				else if (hasFocus && !mouseOver)
				{
					hasFocus = false;
					CheckBlockInput();
					cursorPosition = text.Length;
				}
			}
			else if (StorageGUI.curMouse.RightButton == ButtonState.Pressed && StorageGUI.oldMouse.RightButton == ButtonState.Released)
			{
				if (!mouseOver && Parent != null && hasFocus)
				{
					hasFocus = false;
					cursorPosition = text.Length;
					CheckBlockInput();
				}
				if (mouseOver) {
					Reset();
					StorageGUI.RefreshItems();
					hasFocus = true;
				}
			}

			if (hasFocus)
			{
				PlayerInput.WritingText = true;
				Main.instance.HandleIME();
				string newString = Main.GetInputText(text);
				if ((Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl)) && KeyTyped(Keys.Back))
				{
					newString = string.Empty;
				}
				if (!newString.Equals(text))
				{
					text = newString;
					cursorPosition = text.Length;
					StorageGUI.RefreshItems();
				}
				if (KeyTyped(Keys.Enter) || KeyTyped(Keys.Tab) || KeyTyped(Keys.Escape))
				{
					hasFocus = false;
					CheckBlockInput();
				}
			}
			base.Update(gameTime);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Texture2D texture = ModContent.GetTexture("MagicStorage/SearchBar");
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
			string drawText = isEmpty ? defaultText.Value : text;
			DynamicSpriteFont font = Main.fontMouseText;
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

		private static void CheckBlockInput()
		{
			Main.blockInput = false;
			foreach (UISearchBar searchBar in searchBars)
			{
				if (searchBar.hasFocus)
				{
					Main.blockInput = true;
					break;
				}
			}
		}
	}
}