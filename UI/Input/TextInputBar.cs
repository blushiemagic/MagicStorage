using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Input {
	public abstract class TextInputBar : UIElement, ITextInputElement {
		public TextInputState State { get; }

		public LocalizedText HintText { get; set; }

		public TextInputBar(LocalizedText hintText) {
			_bar ??= MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SearchBar");
			_mouseFont ??= FontAssets.MouseText;

			State = TextInputTracker.ReserveState(this);

			HintText = hintText;
		}

		/// <inheritdoc/>
		public virtual void OnActivityGained() { }

		/// <inheritdoc/>
		public virtual void OnActivityLost() { }

		/// <inheritdoc/>
		public virtual void OnInputChanged() { }

		/// <inheritdoc/>
		public virtual void OnInputCleared() { }

		/// <inheritdoc/>
		public virtual void OnInputFocusGained() { }

		/// <inheritdoc/>
		public virtual void OnInputFocusLost() { }

		void ITextInputElement.Update(GameTime gameTime) => HandleState();

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (!MagicUI.CanUpdateSearchBars || !State.IsActive) {
				RestrictedUpdate(gameTime);
				return;
			}

			HandleState();
		}

		private void HandleState() {
			PreStateTick();

			State.Tick(IsMouseHovering);
		}

		protected virtual void RestrictedUpdate(GameTime gameTime) { }

		protected virtual void PreStateTick() { }

		public override void LeftClick(UIMouseEvent evt) {
			base.LeftClick(evt);
			State.Focus();
		}

		public override void RightClick(UIMouseEvent evt) {
			base.RightClick(evt);
			State.Reset(clearText: !State.HasFocus);
		}

		private const int Padding = 4;
		private static Asset<Texture2D> _bar;
		private static Asset<DynamicSpriteFont> _mouseFont;

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			DrawBackBar(spriteBatch);
			DrawText(spriteBatch);
		}

		private void DrawBackBar(SpriteBatch spriteBatch) {
			Color color = Color.White;
			if (!PreDrawBackBar(spriteBatch, ref color))
				return;

			CalculatedStyle dim = GetDimensions();
			
			Texture2D texture = _bar.Value;
			
			Span<Rectangle> destinations = stackalloc Rectangle[9];
			Span<Rectangle> sources = stackalloc Rectangle[9];
			CalculateBarSlices(dim, destinations, sources);

			for (int i = 0; i < 9; i++)
				spriteBatch.Draw(texture, destinations[i], sources[i], color);
		}

		protected virtual bool PreDrawBackBar(SpriteBatch spriteBatch, ref Color color) => true;

		private void DrawText(SpriteBatch spriteBatch) {
			string text = State.GetCurrentText();
			Color color = Color.Black;
			bool hasText = State.HasText;

			if (!hasText)
				color *= 0.75f;

			if (!PreDrawText(spriteBatch, ref color))
				return;

			CalculatedStyle dim = GetDimensions();
			int innerHeight = (int)dim.Height - 2 * Padding;
			DynamicSpriteFont font = _mouseFont.Value;
			Vector2 size = font.MeasureString(text);
			float scale = innerHeight / size.Y;

			if (!hasText && State.HasFocus) {
				// Remove the hint text
				text = string.Empty;
				hasText = true;
			}

			spriteBatch.DrawString(font, text, new Vector2(dim.X + Padding, dim.Y + Padding), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

			if (hasText && State.CursorBlink) {
				float drawCursor = font.MeasureString(text[..State.CursorLocation]).X * scale;
				spriteBatch.DrawString(font, "|", new Vector2(dim.X + Padding + drawCursor, dim.Y + Padding), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
		}

		protected virtual bool PreDrawText(SpriteBatch spriteBatch, ref Color color) => true;

		private static void CalculateBarSlices(CalculatedStyle dimensions, Span<Rectangle> destinations, Span<Rectangle> sources) {
			int x = (int)dimensions.X;
			int y = (int)dimensions.Y;
			int innerWidth = (int)dimensions.Width - 2 * Padding;
			int innerHeight = (int)dimensions.Height - 2 * Padding;

			destinations[0] = new Rectangle(x,                        y,                         Padding,    Padding);
			destinations[1] = new Rectangle(x + Padding,              y,                         innerWidth, Padding);
			destinations[2] = new Rectangle(x + Padding + innerWidth, y,                         Padding,    Padding);
			destinations[3] = new Rectangle(x,                        y + Padding,               Padding,    innerHeight);
			destinations[4] = new Rectangle(x + Padding,              y + Padding,               innerWidth, innerHeight);
			destinations[5] = new Rectangle(x + Padding + innerWidth, y + Padding,               Padding,    innerHeight);
			destinations[6] = new Rectangle(x,                        y + Padding + innerHeight, Padding,    Padding);
			destinations[7] = new Rectangle(x + Padding,              y + Padding + innerHeight, innerWidth, Padding);
			destinations[8] = new Rectangle(x + Padding + innerWidth, y + Padding + innerHeight, Padding,    Padding);

			sources[0] = new Rectangle(0,           0,           Padding, Padding);
			sources[1] = new Rectangle(Padding,     0,           1,       Padding);
			sources[2] = new Rectangle(Padding + 1, 0,           Padding, Padding);
			sources[3] = new Rectangle(0,           Padding,     Padding, 1);
			sources[4] = new Rectangle(Padding,     Padding,     1,       1);
			sources[5] = new Rectangle(Padding + 1, Padding,     Padding, 1);
			sources[6] = new Rectangle(0,           Padding + 1, Padding, Padding);
			sources[7] = new Rectangle(Padding,     Padding + 1, 1,       Padding);
			sources[8] = new Rectangle(Padding + 1, Padding + 1, Padding, Padding);
		}
	}
}
