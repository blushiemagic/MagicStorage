using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace MagicStorage.UI {
	public class UIPanelTab : UITextPanel<LocalizedText> {
		public readonly string Name;

		public UIPanelTab(string name, LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) {
			Name = name;
		}
	}
}
