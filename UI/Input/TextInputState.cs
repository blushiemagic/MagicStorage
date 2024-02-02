using System;
using System.Text;
using Terraria;

namespace MagicStorage.UI.Input {
	/// <summary>
	/// An object representing the state for an <see cref="ITextInputElement"/>
	/// </summary>
	public class TextInputState {
		private int _cursor;
		private int _timer;
		private bool _focused;

		private bool _active, _oldActive;

		private string _initialText;
		private readonly StringBuilder _text;

		private readonly ITextInputElement _actor;

		public bool HasFocus => _focused;

		public bool HasText => _text.Length > 0;

		public bool HasChanges => _text.ToString() != _initialText;

		public bool IsActive => _active;

		public bool WasActive => _oldActive;

		public int CursorLocation => _cursor;

		public bool CursorBlink => _focused && _timer < 30;

		public string InputText => _text.ToString();

		public ITextInputElement Actor => _actor;

		internal TextInputState(ITextInputElement actor) {
			ArgumentNullException.ThrowIfNull(actor);
			_actor = actor;
			_text = new();
			_initialText = string.Empty;
		}

		public void Tick(bool hoveringMouse) {
			bool oldActive = _oldActive;
			_oldActive = _active;

			if (oldActive != _active) {
				if (_active)
					_actor.OnActivityGained();
				else
					_actor.OnActivityLost();
			}

			if (++_timer >= 60)
				_timer = 0;

			if (!hoveringMouse && (StorageGUI.MouseClicked || StorageGUI.RightMouseClicked))
				Unfocus();

			if (_focused) {
				// Read from IME and update the text
				Main.blockInput = true;
				Main.drawingPlayerChat = false;
				Main.CurrentInputTextTakerOverride = this;

				int length = _text.Length;
				
				BlandFastIME.Handle(_text, ref _cursor);

				if (_text.Length != length)
					_actor.OnInputChanged();

				if (Main.inputTextEnter)
					Unfocus();
				else if (Main.inputTextEscape)
					Reset(clearText: false);
			}
		}

		public void Activate() {
			_active = true;
			_oldActive = false;
		}

		public void Deactivate() {
			_active = false;
			_oldActive = true;
		}

		public void Reset(bool clearText = true) {
			if (!clearText) {
				_text.Clear().Append(_initialText);
				_cursor = _text.Length;
				_actor.OnInputFocusLost();
			} else
				Clear();

			_timer = 0;
			_focused = false;
			TextInputTracker.CheckInputBlocking();
		}

		public void Clear() {
			_text.Clear();
			_initialText = string.Empty;
			_cursor = 0;
			_actor.OnInputCleared();
		}

		public void Set(string text) {
			_text.Clear().Append(text);
			_initialText = text;
			_cursor = _text.Length;
			_actor.OnInputChanged();
		}

		public void Focus() {
			if (!_focused) {
				Main.blockInput = true;
				_focused = true;
				_cursor = _text.Length;
				_actor.OnInputFocusGained();
			}
		}

		public void Unfocus() {
			if (_focused) {
				_focused = false;
				_actor.OnInputFocusLost();
				TextInputTracker.CheckInputBlocking();
				_initialText = _text.ToString();
			}
		}

		public string GetCurrentText() {
			if (_text.Length <= 0)
				return _actor.HintText.Value;

			return _text.ToString();
		}
	}
}
