using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage {
	partial class Utility {
		public static void HoverTextToYellow<T>(UIMouseEvent evt, UIElement e) {
			if (e is UITextPanel<T> panel)
				panel.TextColor = Color.Yellow;
		}

		public static void HoverTextToWhite<T>(UIMouseEvent evt, UIElement e) {
			if (e is UITextPanel<T> panel)
				panel.TextColor = Color.White;
		}
	}
}
