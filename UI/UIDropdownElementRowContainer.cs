using System;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UIDropdownElementRowContainer : UIElement {
		public readonly float padding;

		public UIDropdownElementRowContainer(float padding) {
			this.padding = padding;

			SetPadding(0);
			MarginTop = MarginBottom = MarginLeft = MarginRight = 0;
		}

		public void SetElements(params UIElement[] elements) {
			RemoveAllChildren();

			float left = 0, height = 0;

			foreach (var element in elements) {
				element.Left.Set(left, 0f);

				element.Activate();
				element.Recalculate();

				var dims = element.GetDimensions();

				left += dims.Width + padding;
				height = Math.Max(height, dims.Height);

				Append(element);
			}

			Width.Set(left - padding, 0f);
			Height.Set(height, 0f);

			Recalculate();
		}
	}
}
