using Microsoft.Xna.Framework;
using System;
using Terraria;
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

		public static void AddErrorTextMultiline(ref string destination, string errorText) {
			if (!errorText.Contains('\n')) {
				destination += $"[c/ff0000:{errorText}";
				return;
			}

			ReadOnlySpan<char> span = errorText;
			int lineCount = span.Count('\n') + 1;

			int start = 0;
			for (int i = 0; i < span.Length; i++) {
				ref readonly char current = ref span[i];
				if (current == '\n') {
					destination += $"[c/ff0000:{span[start..i]}\n";
					start = i + 1;
				}
			}

			if (start < span.Length)
				destination += $"[c/ff0000:{span[start..]}";
		}

		public static Item NullItem(int slot, ref int context) {
			context = ItemSlot.Context.InventoryItem;
			return new Item();
		}
	}
}
