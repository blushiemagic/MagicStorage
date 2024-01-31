using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace MagicStorage.UI.Input {
	/// <summary>
	/// Represents an element that can receive text input
	/// </summary>
	public interface ITextInputElement {
		/// <summary>
		/// The current state of the text input element
		/// </summary>
		TextInputState State { get; }

		/// <summary>
		/// The text to display when the text input element is empty
		/// </summary>
		LocalizedText HintText { get; }

		/// <summary>
		/// This method executes whenever <see cref="MagicUI.CanUpdateSearchBars"/> is <see langword="true"/> and the text input element is active
		/// </summary>
		void Update(GameTime gameTime);

		/// <summary>
		/// This method executes whenever the text input element is activated
		/// </summary>
		void OnActivityGained();

		/// <summary>
		/// This method executes whenever the text input element is deactivated
		/// </summary>
		void OnActivityLost();

		/// <summary>
		/// This method executes when the text input element's text has changed
		/// </summary>
		void OnInputChanged();

		/// <summary>
		/// This method executes whenever the text input element's text is cleared
		/// </summary>
		void OnInputCleared();

		/// <summary>
		/// This method executes when the text input element gains focus and starts blocking gameplay input
		/// </summary>
		void OnInputFocusGained();

		/// <summary>
		/// This method executes when the text input element loses focus and stops blocking gameplay input
		/// </summary>
		void OnInputFocusLost();
	}
}
