using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UIResizeButton : UIPanel {
		public bool ResizeWidth { get; set; } = true;

		public bool ResizeHeight { get; set; } = true;

		public bool Dragging { get; set; }

		private Vector2 lastKnownPosition;

		private Vector2 offsetDelta;
		public ref Vector2 OffsetDelta => ref offsetDelta;

		public delegate void RecalculateEvent(UIResizeButton button);

		public event RecalculateEvent OnDragging;

		public UIResizeButton() {
			BorderColor = Color.Transparent;
			BackgroundColor *= 0.2f;

			SetPadding(0);

			Width.Set(24, 0f);
			Height.Set(24, 0f);

			MinWidth = Width;
			MinHeight = Height;

			UIImage icon = new(ModContent.Request<Texture2D>("MagicStorage/Assets/Resize", AssetRequestMode.ImmediateLoad)) {
				HAlign = 0.5f,
				VAlign = 0.5f
			};

			Append(icon);
		}

		public override void LeftMouseDown(UIMouseEvent evt) {
			base.LeftMouseDown(evt);

			DragStart(evt);
		}

		public override void LeftMouseUp(UIMouseEvent evt) {
			base.LeftMouseUp(evt);

			DragEnd(evt);
		}

		private void DragStart(UIMouseEvent evt) {
			if (Dragging || !MagicStorageConfig.CanMoveUIPanels)
				return;

			lastKnownPosition = evt.MousePosition;
			offsetDelta = default;
			Dragging = true;
		}

		private void DragEnd(UIMouseEvent evt) {
			//A child element forced this to not move
			if (!Dragging)
				return;

			Dragging = false;

			UpdateDelta(evt.MousePosition);
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime); // don't remove.

			if ((!ResizeWidth && !ResizeHeight) || !MagicStorageConfig.CanMoveUIPanels)
				Dragging = false;

			if (Dragging)
				UpdateDelta(Main.MouseScreen);
		}

		private void UpdateDelta(Vector2 mouse) {
			offsetDelta = mouse - lastKnownPosition;

			if (!ResizeWidth)
				offsetDelta.X = 0;
			if (!ResizeHeight)
				offsetDelta.Y = 0;

			OnDragging?.Invoke(this);

			lastKnownPosition += offsetDelta;
		}
	}
}
