using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.UI;

namespace MagicStorage.UI.Input {
	public static class TextInputTracker {
		private static readonly List<TextInputState> _inputs = new();

		internal static void Unload() {
			_inputs.Clear();
			Main.blockInput = false;
		}

		public static TextInputState ReserveState<T>(T element) where T : UIElement, ITextInputElement {
			var state = new TextInputState(element);
			_inputs.Add(state);
			return state;
		}

		internal static void CheckInputBlocking() {
			if (_inputs.Find(static s => s.HasFocus) is { } input) {
				Main.CurrentInputTextTakerOverride = input;
				Main.blockInput = true;
			} else
				Main.blockInput = false;
			
			// Always block the chat when calling this method
			Main.drawingPlayerChat = false;
			Main.chatRelease = false;
		}

		internal static void Update(GameTime gameTime) {
			foreach (var input in _inputs)
				((UIElement)input.Actor).Update(gameTime);
		}
	}
}
