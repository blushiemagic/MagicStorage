using Terraria.UI;

namespace MagicStorage.UI {
	public class UIDropdownElementRowContainer : UIElement {
		public readonly float padding;

		public UIDropdownElementRowContainer(float padding) {
			this.padding = padding;
		}

		public void SetElements(params UIElement[] elements) {
			RemoveAllChildren();

			float left = 0;

			foreach (var element in elements) {
				element.Left.Set(left, 0f);

				Append(element);

				left += element.GetDimensions().Width + padding;
			}
		}
	}
}
