using MagicStorage.Common;
using Microsoft.Xna.Framework;
using SerousCommonLib.API.Input;
using SerousCommonLib.UI;
using System;
using Terraria.Localization;

namespace MagicStorage.UI.Selling {
	internal class SellQuantityPrompt : TextInputBar {
		private class IntegerInput : ITextInputController {
			bool ITextInputController.PermitCharacter(char c) {
				// NOTE: SerousCommonLib v1.0.6.2 has an oversight where Enter, Esc and Tab don't bypass this method,
				//       even though they have special behaviors.
				return c is (>= '0' and <= '9') or (char)13 or (char)27 or (char)9;
			}
		}

		public event Action<SellQuantityPrompt> OnQuantityChanged;

		public int Quantity { get; private set; } = 1;

		public int MaximumQuantity { get; set; } = 1;

		public SellQuantityPrompt() : base(Language.GetText("Mods.MagicStorage.StorageGUI.SellQuantityPopup.HintText")) {
			Controller = new IntegerInput();
		}

		public void SetQuantity(int quantity) => SetQuantity(quantity.ToString());

		private string _pendingQuantityText;

		public void SetQuantity(string quantityText) {
			if (!State.IsActive) {
				// Delay the text assignment
				_pendingQuantityText = quantityText;
				return;
			}

			State.Set(quantityText);
			TryValidateQuantityText();
		}

		public override void OnInputEnter() {
			TryValidateQuantityText();
			base.OnInputEnter();
		}

		public override void OnInputFocusLost() {
			if (!_amIResetting)
				TryValidateQuantityText();
			base.OnInputFocusLost();
		}

		public override void Update(GameTime gameTime) {
			// Important note: the prompt may not be active at this point
			if (State.IsActive && _pendingQuantityText is not null) {
				State.Set(_pendingQuantityText);
				TryValidateQuantityText();
				_pendingQuantityText = null;
			}

			base.Update(gameTime);
		}

		private bool _amIResetting = false;
		private void TryValidateQuantityText() {
			if (int.TryParse(State.InputText, out int quantity) && quantity >= 0) {
				// Input was valid
				Quantity = Math.Min(quantity, MaximumQuantity);
			} else {
				if (!State.HasText) {
					// Force the quantity to default to 0
					Quantity = 0;
				} else {
					// Try to revert any changes
					using (FlagSwitch.ToggleTrue(ref _amIResetting))
						State.Reset(clearText: false);

					if (int.TryParse(State.InputText, out quantity)) {
						// Previous input was valid
						Quantity = Math.Min(quantity, MaximumQuantity);
					} else {
						// Force the quantity to default to 0
						Quantity = 0;
					}
				}
			}

			// Ensure that the text matches the actual value
			State.Set(Quantity.ToString());

			OnQuantityChanged?.Invoke(this);
		}
	}
}
