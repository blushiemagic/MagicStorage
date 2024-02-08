using System.Text;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace MagicStorage.UI.Selling {
	public class SellConfirmButton : UITextPanel<LocalizedText> {
		private readonly UIText _coinReport;

		private const float COIN_REPORT_SCALE = 0.8f;

		// Each item tag is 32 pixels wide, and there are an additional 3 spaces between each tag
		private static float CoinReportMaxWidth => (32f * 4 + FontAssets.MouseText.Value.MeasureString(" ").X * 3) * COIN_REPORT_SCALE;

		public SellConfirmButton(float textScale = 1, bool large = false) : base(Language.GetText("LegacyTooltip.49"), textScale, large) {
			Height.Set(MinHeight.Pixels, 0f);
			PaddingTop = 8;

			TextHAlign = 0f;

			_coinReport = new UIText("", COIN_REPORT_SCALE, false) {
				VAlign = 0.2f
			};
			SetCoins(default);
			Append(_coinReport);
		}

		public override void SetText(LocalizedText text, float textScale, bool large) {
			base.SetText(text, textScale, large);

			MinWidth.Set(MinWidth.Pixels + CoinReportMaxWidth, 0f);
		}

		public void SetCoins(SellModeMetadata.Coins coins) {
			string str = GetCoinString(coins);

			if (_coinReport.Text != str) {
				_coinReport.SetText(str);
				_coinReport.Left.Set(-_coinReport.MinWidth.Pixels, 1f);
				_coinReport.Recalculate();
			}
		}

		private static string GetCoinString(SellModeMetadata.Coins coins) {
			if (coins.TotalValue == 0)
				return Language.GetTextValue("LegacyInterface.23");

			StringBuilder sb = new();
			if (coins.platinum > 0)
				sb.Append($"[i/s{coins.platinum}:PlatinumCoin] ");
			if (coins.gold > 0)
				sb.Append($"[i/s{coins.gold}:GoldCoin] ");
			if (coins.silver > 0)
				sb.Append($"[i/s{coins.silver}:SilverCoin] ");
			if (coins.copper > 0)
				sb.Append($"[i/s{coins.copper}:CopperCoin] ");

			sb.Length--;

			return sb.ToString();
		}
	}
}
