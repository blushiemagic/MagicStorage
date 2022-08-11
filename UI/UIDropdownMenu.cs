using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class UIDropdownMenu : UIPanel {
		private class UIViewArea : UIPanel {
			public override bool ContainsPoint(Vector2 point) => true;

			protected override void DrawChildren(SpriteBatch spriteBatch) {
				var parentDims = Parent.GetDimensions();

				Vector2 position = parentDims.Position();
				Vector2 dimensions = new(parentDims.Width, parentDims.Height);

				foreach (UIElement element in Elements) {
					var elementDims = element.GetDimensions();

					Vector2 position2 = elementDims.Position();
					Vector2 dimensions2 = new(elementDims.Width, elementDims.Height);

					if (Collision.CheckAABBvAABBCollision(position, dimensions, position2, dimensions2))
						element.Draw(spriteBatch);
				}
			}

			public override Rectangle GetViewCullingArea() => Parent.GetDimensions().ToRectangle();
		}

		public readonly NewUIList list;
		public readonly NewUIScrollbar scroll;

		private readonly UIPanel header;
		private readonly UIViewArea viewArea;
		private readonly UIText caption;
		private readonly UITextPanel<char> arrow;

		private float listHeightFactor;
		private readonly float fullDropdownSize;

		private bool expanding;

		public UIDropdownMenu(string captionText, float width, int listPadding, float fullDropdownSize) {
			Width.Set(width, 0f);
			MinWidth = Width;

			this.fullDropdownSize = fullDropdownSize;

			SetPadding(0);

			header = new();
			header.SetPadding(0);
			header.Width.Set(0, 1f);
			header.Height.Set(30, 0f);
			header.BackgroundColor.A = 255;
			header.OnClick += (evt, e) => HeaderClicked();
			Append(header);

			caption = new(captionText);
			caption.VAlign = 0.5f;
			caption.Left.Set(8f, 0f);

			//Initial height
			MinHeight = Height = header.Height;

			header.Append(caption);

			arrow = new('>');
			arrow.SetPadding(7);
			arrow.Width.Set(40, 0);
			arrow.Left.Set(-40, 1);
			arrow.BackgroundColor = Color.Transparent;
			arrow.BorderColor = Color.Transparent;
			header.Append(arrow);

			viewArea = new();
			viewArea.Top.Set(header.Height.Pixels, 0);
			viewArea.Width.Set(0, 1f);
			viewArea.Height.Set(fullDropdownSize, 0f);
			viewArea.BackgroundColor = Color.Transparent;
			viewArea.BorderColor = Color.Transparent;
			viewArea.PaddingLeft = viewArea.PaddingRight = 0;
			Append(viewArea);

			list = new();
			list.SetPadding(0);
			list.Width.Set(-20, 1f);
			list.Height.Set(0f, 1f);
			viewArea.Append(list);

			scroll = new();
			scroll.Width.Set(20, 0);
			scroll.Height.Set(0, 0.825f);
			scroll.Left.Set(-20, 1f);
			scroll.Top.Set(0, 0.1f);

			list.SetScrollbar(scroll);
			list.Append(scroll);
			list.ListPadding = listPadding;
		}

		private void HeaderClicked() {
			SoundEngine.PlaySound(SoundID.MenuTick);

			if (!expanding)
				arrow.SetText('v');
			else
				arrow.SetText('>');

			expanding = !expanding;
		}

		public void Reset() {
			expanding = false;
			arrow.SetText('>');
			listHeightFactor = 0f;
			scroll.ViewPosition = 0f;
		}

		public override void Update(GameTime gameTime) {
			scroll.SetView(0f, list.Count);
			
			base.Update(gameTime);

			float old = listHeightFactor;

			const float ticks = 22f;

			if (expanding) {
				listHeightFactor += 1f / ticks;

				if (listHeightFactor > 1f)
					listHeightFactor = 1f;
			} else {
				listHeightFactor -= 1f / ticks;

				if (listHeightFactor < 0f)
					listHeightFactor = 0f;
			}

			if (old != listHeightFactor) {
				Height.Set(header.Height.Pixels + fullDropdownSize * listHeightFactor, 0f);
				Recalculate();
			}
		}

		protected override void DrawChildren(SpriteBatch spriteBatch) {
			header.Draw(spriteBatch);

			if (listHeightFactor <= 0f)
				return;

			viewArea.Draw(spriteBatch);
		}

		public void Add(UIElement element) => list.Add(element);

		public void AddRange(IEnumerable<UIElement> elements) => list.AddRange(elements);

		public bool Remove(UIElement element) => list.Remove(element);

		public void Clear() {
			List<UIElement> copy = new(list._items);

			foreach (var element in copy)
				list.Remove(element);
		}
	}
}
