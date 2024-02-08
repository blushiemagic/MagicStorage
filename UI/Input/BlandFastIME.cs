using Microsoft.Xna.Framework.Input;
using ReLogic.Localization.IME;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace MagicStorage.UI.Input {
	internal class BlandFastIME : ModSystem {
		private static bool _hasIMEListener;

		public override void Load() {
			if (!Main.dedServ) {
				Platform.Get<IImeService>().AddKeyListener(listener: Listen);
				_hasIMEListener = true;
			}
		}

		public override void Unload() {
			if (_hasIMEListener) {
				Platform.Get<IImeService>().RemoveKeyListener(listener: Listen);
				_hasIMEListener = false;
			}
		}

		private static readonly Queue<char> _inputQueue = new();

		private static int backSpaceCount;
		private static float backSpaceRate;

		private static void Listen(char key) {
			// Force vanilla to be cleared when this system is active, and vice versa
			if (TextInputTracker.GetFocusedState() is not null) {
				Main.keyCount = 0;

				_inputQueue.Enqueue(key);

				// Limit the queue to 10 characters, destroying the oldest ones
				if (_inputQueue.Count > 10)
					_inputQueue.Dequeue();
			} else {
				_inputQueue.Clear();
			}
		}

		public static void Handle(StringBuilder text) {
			int cursor = text.Length;
			Handle(text, ref cursor);
		}
		
		public const int VK_TAB = 0x09;
		public const int VK_ENTER = 0x0D;
		public const int VK_ESCAPE = 0x1B;

		// Main.GetInputText(), but changed to not suck
		// Magic Storage doesn't need the chat tags system for this, so removing it is fine
		public static void Handle(StringBuilder text, ref int cursor) {
			// These two lines are important for enabling the key listener event above
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();

			if (!Main.hasFocus)
				return;

			Main.inputTextEnter = false;
			Main.inputTextEscape = false;
			
			Main.oldInputText = Main.inputText;
			Main.inputText = Keyboard.GetState();

			// Handle control sequences for clearing, copying, cutting and pasting
			if (Main.inputText.PressingControl()) {
				if (KeyTyped(Keys.Z)) {
					// Clear the text
					text.Length = 0;
					cursor = 0;
					goto cleanup;
				} else if (KeyTyped(Keys.X)) {
					// Cut: Ctrl+X
					Platform.Get<IClipboard>().Value = text.ToString();
					text.Length = 0;
					cursor = 0;
					goto cleanup;
				} else if (KeyTyped(Keys.C) || KeyTyped(Keys.Insert)) {
					// Copy: Ctrl+C or Ctrl+Insert
					Platform.Get<IClipboard>().Value = text.ToString();
					goto cleanup;
				} else if (KeyTyped(Keys.V)) {
					// Paste: Ctrl+V
					string paste = Platform.Get<IClipboard>().Value;
					text.Insert(cursor, paste);
					cursor += paste.Length;
					goto cleanup;
				} else if (KeyTyped(Keys.Back)) {
					// Delete the last "word" in the text
					ReadOnlySpan<char> currentText = text.ToString();
					
					int i;
					// Look for the first non-whitespace character
					for (i = cursor; i >= 0; i--) {
						if (!char.IsWhiteSpace(currentText[i]))
							break;
					}
					// Look for the first whitespace character
					for (; i >= 0; i--) {
						if (char.IsWhiteSpace(currentText[i]))
							break;
					}

					if (i < 0) {
						// Delete everything starting at the cursor
						text.Remove(0, cursor);
						cursor = 0;
					} else {
						// Delete everthing between the found whitespace and the cursor
						int nonWS = i + 1;
						text.Remove(nonWS, cursor - nonWS);
						cursor = nonWS;
					}

					goto cleanup;
				}
			} else if (Main.inputText.PressingShift()) {
				if (KeyTyped(Keys.Delete)) {
					// Cut: Shift+Delete
					Platform.Get<IClipboard>().Value = text.ToString();
					text.Length = 0;
					cursor = 0;
					goto cleanup;
				} else if (KeyTyped(Keys.Insert)) {
					// Paste: Shift+Insert
					string paste = Platform.Get<IClipboard>().Value;
					text.Insert(cursor, paste);
					cursor += paste.Length;
					goto cleanup;
				}
			}

			// Handle character input, single character deletion via Backspace and cursor manipulation
			bool canDeleteAKey = KeyTyped(Keys.Back);

			if (KeyHeld(Keys.Back)) {
				backSpaceRate -= 0.05f;
				if (backSpaceRate < 0f)
					backSpaceRate = 0f;

				if (backSpaceCount <= 0) {
					backSpaceCount = (int)Math.Round(backSpaceRate);
					canDeleteAKey = true;
				}

				backSpaceCount--;
			} else {
				backSpaceRate = 7f;
				backSpaceCount = 15;
			}

			if (KeyTyped(Keys.Left) && cursor > 0) {
				cursor--;
				goto cleanup;
			} else if (KeyTyped(Keys.Right)) {
				cursor++;
				goto cleanup;
			} else if (KeyTyped(Keys.Home)) {
				cursor = 0;
				goto cleanup;
			} else if (KeyTyped(Keys.End)) {
				cursor = text.Length;
				goto cleanup;
			} else if (canDeleteAKey && cursor > 0) {
				text.Remove(--cursor, 1);
				goto cleanup;
			}

			// Process the character queue
			while (_inputQueue.TryDequeue(out char c)) {
				if (c == VK_ENTER)
					Main.inputTextEnter = true;
				else if (c == VK_ESCAPE || c == VK_TAB)
					Main.inputTextEscape = true;
				else if (c >= 32 && c != 127)
					text.Insert(cursor++, c);
			}

			cleanup:
			// Always empty the queue after executing this handler
			_inputQueue.Clear();
		}

		private static bool KeyHeld(Keys key) => Main.inputText.IsKeyDown(key) && Main.oldInputText.IsKeyDown(key);

		private static bool KeyTyped(Keys key) => Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
	}
}
