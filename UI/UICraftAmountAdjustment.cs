using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace MagicStorage.UI {
	internal class UICraftAmountAdjustment : UITextPanel<LocalizedText> {
		public int Amount { get; private set; }

		public bool AmountIsOffset { get; private set; }

		public UICraftAmountAdjustment(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

		public void SetAmountInformation(int amount, bool amountIsOffset) {
			Amount = amount;
			AmountIsOffset = amountIsOffset;
		}
	}
}
