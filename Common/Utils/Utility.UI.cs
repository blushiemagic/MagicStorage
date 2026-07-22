using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Changes hovered text-panel text to yellow.
		/// </summary>
		/// <typeparam name="T">The text-panel value type.</typeparam>
		/// <param name="evt">The mouse event.</param>
		/// <param name="e">The hovered UI element.</param>
		public static void HoverTextToYellow<T>(UIMouseEvent evt, UIElement e) {
			if (e is UITextPanel<T> panel)
				panel.TextColor = Color.Yellow;
		}

		/// <summary>
		/// Changes hovered text-panel text to white.
		/// </summary>
		/// <typeparam name="T">The text-panel value type.</typeparam>
		/// <param name="evt">The mouse event.</param>
		/// <param name="e">The hovered UI element.</param>
		public static void HoverTextToWhite<T>(UIMouseEvent evt, UIElement e) {
			if (e is UITextPanel<T> panel)
				panel.TextColor = Color.White;
		}

		/// <summary>
		/// Appends red Terraria chat markup for single-line or multiline error text.
		/// </summary>
		/// <param name="destination">The destination string to append to.</param>
		/// <param name="errorText">The error text to append.</param>
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

		/// <summary>
		/// Returns an empty item and resets the item-slot context to inventory.
		/// </summary>
		/// <param name="slot">The item slot index.</param>
		/// <param name="context">The item-slot context to update.</param>
		/// <returns>A new empty item.</returns>
		public static Item NullItem(int slot, ref int context) {
			context = ItemSlot.Context.InventoryItem;
			return new Item();
		}
	}
}
