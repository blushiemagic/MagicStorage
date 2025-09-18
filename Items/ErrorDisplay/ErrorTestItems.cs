using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items.ErrorDisplay {
	[Autoload(BaseErrorDummyItem.LoadTestItems)]
	internal class ErrorTestRenderFail : ModItem {
		public override string Texture => base.Texture.Replace(nameof(ErrorTestRenderFail), "Error_ItemSlotRenderFail");

		public override LocalizedText DisplayName => Mod.GetLocalization("HoverText.TestItems.RenderFail");

		public override LocalizedText Tooltip => Mod.GetLocalization("HoverText.TestItems.NoTooltip");
	}

	[Autoload(BaseErrorDummyItem.LoadTestItems)]
	internal class ErrorTestClientReadFail : ModItem {
		public override string Texture => base.Texture.Replace(nameof(ErrorTestClientReadFail), "Error_ItemNetReadFail");

		public override LocalizedText DisplayName => Mod.GetLocalization("HoverText.TestItems.NetFail");

		public override LocalizedText Tooltip => Mod.GetLocalization("HoverText.TestItems.NoTooltip");
	}
}
