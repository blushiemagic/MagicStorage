using Terraria.ID;
using Terraria.Localization;

namespace MagicStorage.UI {
	public class UIShimmerButton : UICraftButton {
		public UIShimmerButton(LocalizedText text, string hoverTextKey) : base(text, hoverTextKey) { }

		protected override bool DisplayHoverText() => DecraftingGUI.selectedItem > ItemID.None;

		protected override void HandleCraft(ref bool stillCrafting) => DecraftingGUI.ClickShimmerButton(ref stillCrafting);
	}
}
