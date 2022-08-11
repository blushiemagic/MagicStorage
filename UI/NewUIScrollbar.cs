using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI {
	//A copy of Terraria's UIScrollbar, but with a few things exposed and modified slightly for convenience
	public class NewUIScrollbar : UIElement {
		private float _viewPosition;

		public float ViewSize { get; private set; } = 1f;
		public float MaxViewSize { get; private set; } = 20f;
		
		public bool IsDragging { get; private set; }
		
		private bool _isHoveringOverHandle;
		private float _dragYOffset;
		private readonly Asset<Texture2D> _texture;
		private readonly Asset<Texture2D> _innerTexture;

		public event Action<NewUIScrollbar> OnDraggingStart, OnDraggingEnd;

		public float ViewPosition {
			get {
				return _viewPosition;
			}
			set {
				_viewPosition = MathHelper.Clamp(value, 0f, MaxViewSize - ViewSize);
			}
		}

		public bool CanScroll => MaxViewSize != ViewSize;

		public void GoToBottom() {
			ViewPosition = MaxViewSize - ViewSize;
		}

		public NewUIScrollbar() {
			Width.Set(20f, 0f);
			MaxWidth.Set(20f, 0f);
			_texture = Main.Assets.Request<Texture2D>("Images/UI/Scrollbar");
			_innerTexture = Main.Assets.Request<Texture2D>("Images/UI/ScrollbarInner");
			PaddingTop = 5f;
			PaddingBottom = 5f;
		}

		public void SetView(float viewSize, float maxViewSize) {
			viewSize = MathHelper.Clamp(viewSize, 0f, maxViewSize);
			_viewPosition = MathHelper.Clamp(_viewPosition, 0f, maxViewSize - viewSize);
			ViewSize = viewSize;
			MaxViewSize = maxViewSize;
		}

		public Rectangle GetHandleRectangle() {
			CalculatedStyle innerDimensions = GetInnerDimensions();
			if (MaxViewSize == 0f && ViewSize == 0f) {
				ViewSize = 1f;
				MaxViewSize = 1f;
			}

			return new Rectangle((int)innerDimensions.X, (int)(innerDimensions.Y + innerDimensions.Height * (_viewPosition / MaxViewSize)) - 3, 20, (int)(innerDimensions.Height * (ViewSize / MaxViewSize)) + 7);
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			CalculatedStyle innerDimensions = GetInnerDimensions();
			if (IsDragging) {
				float num = UserInterface.ActiveInstance.MousePosition.Y - innerDimensions.Y - _dragYOffset;
				_viewPosition = MathHelper.Clamp(num / innerDimensions.Height * MaxViewSize, 0f, MaxViewSize - ViewSize);
			}

			Rectangle handleRectangle = GetHandleRectangle();
			Vector2 mousePosition = UserInterface.ActiveInstance.MousePosition;
			bool isHoveringOverHandle = _isHoveringOverHandle;
			_isHoveringOverHandle = handleRectangle.Contains(new Point((int)mousePosition.X, (int)mousePosition.Y));
			if (!isHoveringOverHandle && _isHoveringOverHandle && Main.hasFocus && !IsDragging)
				SoundEngine.PlaySound(SoundID.MenuTick);
		}

		internal static void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color color) {
			spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y - 6, dimensions.Width, 6), new Rectangle(0, 0, texture.Width, 6), color);
			spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y, dimensions.Width, dimensions.Height), new Rectangle(0, 6, texture.Width, 4), color);
			spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y + dimensions.Height, dimensions.Width, 6), new Rectangle(0, texture.Height - 6, texture.Width, 6), color);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			CalculatedStyle dimensions = GetDimensions();
			Rectangle handleRectangle = GetHandleRectangle();

			DrawBar(spriteBatch, _texture.Value, dimensions.ToRectangle(), Color.White);
			DrawBar(spriteBatch, _innerTexture.Value, handleRectangle, Color.White * ((IsDragging || _isHoveringOverHandle) ? 1f : 0.85f));
		}

		public override void MouseDown(UIMouseEvent evt) {
			base.MouseDown(evt);

			if (evt.Target == this) {
				Rectangle handleRectangle = GetHandleRectangle();
				if (handleRectangle.Contains(new Point((int)evt.MousePosition.X, (int)evt.MousePosition.Y))) {
					if (!IsDragging)
						OnDraggingStart?.Invoke(this);

					IsDragging = true;
					_dragYOffset = evt.MousePosition.Y - handleRectangle.Y;
				} else {
					CalculatedStyle innerDimensions = GetInnerDimensions();
					float num = UserInterface.ActiveInstance.MousePosition.Y - innerDimensions.Y - (handleRectangle.Height >> 1);
					_viewPosition = MathHelper.Clamp(num / innerDimensions.Height * MaxViewSize, 0f, MaxViewSize - ViewSize);
				}
			}
		}

		public override void MouseUp(UIMouseEvent evt) {
			base.MouseUp(evt);

			if (IsDragging)
				OnDraggingEnd?.Invoke(this);

			IsDragging = false;
		}

		public override void ScrollWheel(UIScrollWheelEvent evt) {
			if (IsDragging)
				return;

			base.ScrollWheel(evt);

			_viewPosition -= evt.ScrollWheelValue / 250f;
		}
	}
}
