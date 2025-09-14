using Terraria.UI;

namespace MagicStorage {
	partial class Utility {
		public static void ClearPaddingAndMargins(this UIElement element) {
			element.SetPadding(0);
			element.MarginTop = element.MarginLeft = element.MarginRight = element.MarginBottom = 0;
		}

		public static void ClearMargins(this UIElement element) {
			element.MarginTop = element.MarginLeft = element.MarginRight = element.MarginBottom = 0;
		}

		public static void RemoveAndDeactivate(this UIElement element) {
			if (element is not null) {
				element.Remove();
				element.Deactivate();
			}
		}
	}
}
