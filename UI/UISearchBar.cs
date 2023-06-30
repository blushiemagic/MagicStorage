using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI
{
	public class UISearchBar : UIElement
	{
		private const int Padding = 4;
		private static readonly List<UISearchBar> _searchBars = new();
		private static Asset<Texture2D> TextureAsset;
		private static Asset<DynamicSpriteFont> MouseTextFont;
		private readonly Action _clearedEvent;
		private LocalizedText defaultText;
		private int cursorPosition;
		private int cursorTimer;
		private bool hasFocus;

		internal static IReadOnlyList<UISearchBar> SearchBars => _searchBars;

		public string Text { get; private set; } = string.Empty;

		internal bool active, oldActive;

		public Func<string> GetHoverText { get; set; }

		public UISearchBar(LocalizedText defaultText, Action clearedEvent)
		{
			TextureAsset ??=  MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SearchBar", AssetRequestMode.ImmediateLoad);
			MouseTextFont ??= FontAssets.MouseText;

			SetPadding(Padding);
			_searchBars.Add(this);
			this.defaultText = defaultText;
			_clearedEvent = clearedEvent;
		}

		public void SetDefaultText(LocalizedText defaultText) {
			this.defaultText = defaultText;
		}

		public void Reset()
		{
			Text = string.Empty;
			cursorPosition = 0;
			hasFocus = false;
			CheckBlockInput();
			oldMouseOver = false;
		}

		private bool oldMouseOver;

		public override void Update(GameTime gameTime)
		{
			//Unfortunately, I can't convert this to the new API
			//The click events only run on the element being clicked, for obvious reasons
			// -- absoluteAquarian
			Rectangle dim = InterfaceHelper.GetFullRectangle(this);
			MouseState mouse = StorageGUI.curMouse;
			bool mouseOver = mouse.X > dim.X && mouse.X < dim.X + dim.Width && mouse.Y > dim.Y && mouse.Y < dim.Y + dim.Height;

			bool oldMouseOver = this.oldMouseOver;
			this.oldMouseOver = mouseOver;

			bool oldActive = this.oldActive;
			this.oldActive = active;

			//Hack to give search bars special update logic since they have to update in ModSystem.PostUpdateInput instead of ModSystem.UpdateUI
			if (!MagicUI.CanUpdateSearchBars || !active) {
				if (active) {
					if (mouseOver && GetHoverText?.Invoke() is string s) {
						if (MagicUI.lastKnownSearchBarErrorReason is not null && !StorageGUI.CurrentlyRefreshing)
							s += $"\n[c/ff0000:{MagicUI.lastKnownSearchBarErrorReason}]";

						if (!string.IsNullOrWhiteSpace(s))
							MagicUI.mouseText = s;
						else
							MagicUI.mouseText = "";
					} else if (oldMouseOver && !mouseOver)
						MagicUI.mouseText = "";
				} else if (oldActive)
					MagicUI.mouseText = "";

				return;
			}

			cursorTimer++;
			cursorTimer %= 60;

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

				Click(new(this, UserInterface.ActiveInstance.MousePosition));
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
			else if (mouseOver)
			{
				if (Text.Length > 0)
				{
					Text = string.Empty;
					cursorPosition = 0;
					_clearedEvent?.Invoke();
				}

				base.RightClick(new(this, UserInterface.ActiveInstance.MousePosition));
			}
		}

		internal void LoseFocus(bool forced = false)
		{
			if (!hasFocus)
				return;

			hasFocus = false;
			CheckBlockInput();
			cursorPosition = Text.Length;

			if (forced || !MagicStorageConfig.SearchBarRefreshOnKey)
				StorageGUI.SetRefresh(forceFullRefresh: true);
		}

		private void HandleTextInput()
		{
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			string prev = Text;
			if (cursorPosition < Text.Length && Text.Length > 0)
				prev = prev.Remove(cursorPosition);

			string newString = Main.GetInputText(prev);
			if ((Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl)) && KeyTyped(Keys.Back))
				DeleteWord(ref newString);

			if (newString != prev)
			{
				int newStringLength = newString.Length;
				if (prev != Text)
					newString += Text[cursorPosition..];
				Text = newString;
				cursorPosition = newStringLength;

				if (MagicStorageConfig.SearchBarRefreshOnKey)
					StorageGUI.SetRefresh(forceFullRefresh: true);
			}

			if (KeyTyped(Keys.Delete) && Text.Length > 0 && cursorPosition < Text.Length)
			{
				Text = Text.Remove(cursorPosition, 1);

				if (MagicStorageConfig.SearchBarRefreshOnKey)
					StorageGUI.SetRefresh(forceFullRefresh: true);
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

				if (!MagicStorageConfig.SearchBarRefreshOnKey)
					StorageGUI.SetRefresh(forceFullRefresh: true);
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
			if (MagicUI.lastKnownSearchBarErrorReason is not null && !StorageGUI.CurrentlyRefreshing)
				color = Color.Red;
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
			Main.blockInput = _searchBars.Any(searchBar => searchBar.hasFocus);
		}

		internal static void ClearList() {
			_searchBars.Clear();
		}
	}
}
