using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI
{
	public class UISearchBar : UIElement
	{
		private const int Padding = 4;
		private static readonly List<UISearchBar> SearchBars = new();
		private static readonly Asset<Texture2D> TextureAsset = MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SearchBar", AssetRequestMode.ImmediateLoad);
		private static readonly Asset<DynamicSpriteFont> MouseTextFont = FontAssets.MouseText;
		private readonly Action _clearedEvent;
		private readonly LocalizedText defaultText;
		private int cursorPosition;
		private int cursorTimer;
		private bool hasFocus;

		public string Text { get; private set; } = string.Empty;

		public UISearchBar(LocalizedText defaultText, Action clearedEvent)
		{
			SetPadding(Padding);
			SearchBars.Add(this);
			this.defaultText = defaultText;
			_clearedEvent = clearedEvent;
		}

		public void Reset()
		{
			Text = string.Empty;
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
			if (StorageGUI.MouseClicked && Parent is not null)
				LeftClick(mouseOver);
			else if (StorageGUI.RightMouseClicked)
				RightClick(mouseOver);

			if (hasFocus)
				HandleTextInput();

			base.Update(gameTime);
		}

		private void LeftClick(bool mouseOver)
		{
			if (!hasFocus && mouseOver)
			{
				hasFocus = true;
				CheckBlockInput();
			}
			else if (hasFocus && !mouseOver)
			{
				LoseFocus();
			}
		}

		private void RightClick(bool mouseOver)
		{
			if (!mouseOver && Parent is not null && hasFocus)
			{
				LoseFocus();
			}
			else if (mouseOver && Text.Length > 0)
			{
				Text = string.Empty;
				cursorPosition = 0;
				_clearedEvent?.Invoke();
			}
		}

		private void LoseFocus()
		{
			hasFocus = false;
			CheckBlockInput();
			cursorPosition = Text.Length;
		}

		private void HandleTextInput()
		{
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			string prev = Text;
			if (cursorPosition < Text.Length && Text.Length > 0)
				prev = prev.Remove(cursorPosition);

			string newString = Main.GetInputText(prev);


			if (Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl))
			{
				if (KeyTyped(Keys.Back))
					DeleteWord(ref newString);
				else if (KeyTyped(Keys.Left))
					HandleLeft();
				else if (KeyTyped(Keys.Right))
					HandleRight();
			}

			if (newString != prev)
			{
				int newStringLength = newString.Length;
				if (prev != Text)
					newString += Text[cursorPosition..];
				Text = newString;
				cursorPosition = newStringLength;
				StorageGUI.RefreshItems();
			}

			if (KeyTyped(Keys.Delete) && Text.Length > 0 && cursorPosition < Text.Length)
			{
				Text = Text.Remove(cursorPosition, 1);
				StorageGUI.RefreshItems();
			}

			if (KeyTyped(Keys.Left) && cursorPosition > 0)
				cursorPosition--;
			if (KeyTyped(Keys.Right) && cursorPosition < Text.Length)
				cursorPosition++;
			if (KeyTyped(Keys.Home))
				cursorPosition = 0;
			if (KeyTyped(Keys.End))
				cursorPosition = Text.Length;
			if (KeyTyped(Keys.Enter) || KeyTyped(Keys.Tab) || KeyTyped(Keys.Escape))
			{
				hasFocus = false;
				CheckBlockInput();
			}
		}

		private void HandleRight()
		{
			if (cursorPosition != Text.Length)
			{
				//Check if first character is Whitespace and skip if so
				if (char.IsWhiteSpace(Text[cursorPosition]))
				{
					cursorPosition += 1;
				}

				var newPos = Text.IndexOf(' ', cursorPosition);

				if (newPos == -1)
				{
					cursorPosition = Text.Length - 1;
				}
				else
				{
					cursorPosition = newPos - 1;
				}
			}
		}

		private void HandleLeft()
		{
			if (cursorPosition > 0)
			{
				var newPos = Text.Substring(0, cursorPosition).TrimEnd().LastIndexOf(' ');

				if (newPos == -1)
				{
					cursorPosition = 0;
				}
				else
				{
					cursorPosition = newPos + 1;
				}
			}
		}



		private static void DeleteWord(ref string newString)
		{
			string trimmed = newString.TrimEnd();
			int index = trimmed.LastIndexOf(" ", trimmed.Length, StringComparison.Ordinal);
			newString = index != -1 ? trimmed.Substring(0, index) : string.Empty;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dim = GetDimensions();
			int innerWidth = (int) dim.Width - 2 * Padding;
			int innerHeight = (int) dim.Height - 2 * Padding;
			Texture2D texture = TextureAsset.Value;
			spriteBatch.Draw(texture, dim.Position(), new Rectangle(0, 0, Padding, Padding), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int) dim.X + Padding, (int) dim.Y, innerWidth, Padding), new Rectangle(Padding, 0, 1, Padding), Color.White);
			spriteBatch.Draw(texture, new Vector2(dim.X + Padding + innerWidth, dim.Y), new Rectangle(Padding + 1, 0, Padding, Padding), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int) dim.X, (int) dim.Y + Padding, Padding, innerHeight), new Rectangle(0, Padding, Padding, 1), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int) dim.X + Padding, (int) dim.Y + Padding, innerWidth, innerHeight), new Rectangle(Padding, Padding, 1, 1),
				Color.White);
			spriteBatch.Draw(texture, new Rectangle((int) dim.X + Padding + innerWidth, (int) dim.Y + Padding, Padding, innerHeight),
				new Rectangle(Padding + 1, Padding, Padding, 1), Color.White);
			spriteBatch.Draw(texture, new Vector2(dim.X, dim.Y + Padding + innerHeight), new Rectangle(0, Padding + 1, Padding, Padding), Color.White);
			spriteBatch.Draw(texture, new Rectangle((int) dim.X + Padding, (int) dim.Y + Padding + innerHeight, innerWidth, Padding),
				new Rectangle(Padding, Padding + 1, 1, Padding), Color.White);
			spriteBatch.Draw(texture, new Vector2(dim.X + Padding + innerWidth, dim.Y + Padding + innerHeight), new Rectangle(Padding + 1, Padding + 1, Padding, Padding),
				Color.White);

			bool isEmpty = Text.Length == 0;
			string drawText = isEmpty ? defaultText.Value : Text;
			DynamicSpriteFont font = MouseTextFont.Value;
			Vector2 size = font.MeasureString(drawText);
			float scale = innerHeight / size.Y;
			if (isEmpty && hasFocus)
			{
				drawText = string.Empty;
				isEmpty = false;
			}

			Color color = Color.Black;
			if (isEmpty)
				color *= 0.75f;
			spriteBatch.DrawString(font, drawText, new Vector2(dim.X + Padding, dim.Y + Padding), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			if (!isEmpty && hasFocus && cursorTimer < 30)
			{
				float drawCursor = font.MeasureString(drawText.Substring(0, cursorPosition)).X * scale;
				spriteBatch.DrawString(font, "|", new Vector2(dim.X + Padding + drawCursor, dim.Y + Padding), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
		}

		public static bool KeyTyped(Keys key) => Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);

		private static void CheckBlockInput()
		{
			Main.blockInput = SearchBars.Any(searchBar => searchBar.hasFocus);
		}
	}
}
