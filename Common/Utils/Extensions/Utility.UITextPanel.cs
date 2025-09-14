using Terraria.GameContent.UI.Elements;

namespace MagicStorage {
	partial class Utility {
		public static void SetBasicHoverColorChangeEvents<T>(this UITextPanel<T> panel) {
			panel.OnMouseOver += HoverTextToYellow<T>;
			panel.OnMouseOut += HoverTextToWhite<T>;
		}
	}
}
