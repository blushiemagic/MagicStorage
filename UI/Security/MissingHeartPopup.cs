using MagicStorage.Common.Systems;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Security {
	internal class MissingHeartPopup : UIPanel {
	//	private readonly BaseOrderedLayout _layout;
		private readonly UIElement _layout;
		private readonly UIText _header, _body, _body2;

		public MissingHeartPopup(SecuritySystem.NetworkView view) {
			BackgroundColor = Utility.PanelColorWithoutTransparency;
			SetPadding(pixels: 8);
			this.ClearMargins();

			HAlign = 0.5f;
			VAlign = 0.5f;
			Width.Set(-80, 1f);

			_header = new UIText(Language.GetText("Mods.MagicStorage.Security.UI.NoHeartAccessDenied"), large: true);
			_header.Top.Set(8, 0f);
			_header.HAlign = 0.5f;

			Append(_header);

			_body = new UIText(Language.GetText("Mods.MagicStorage.Security.UI.NoHeartMustConnect1"));
			_body.Top.Set(_header.MinHeight.Pixels + 24, 0f);
			_body.HAlign = 0.5f;

			Append(_body);

			_body2 = new UIText(Language.GetText("Mods.MagicStorage.Security.UI.NoHeartMustConnect2").Format(view.id));
			_body2.Top.Set(_body.Top.Pixels + _body.MinHeight.Pixels + 8, 0f);
			_body2.HAlign = 0.5f;

			Append(_body2);

			Height.Set(_body2.Top.Pixels + _body2.MinHeight.Pixels + PaddingTop + PaddingBottom, 0f);
		}
	}
}
