using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public class DecraftingAccess : StorageAccess {
		public override TEDecraftingAccess GetTileEntity() => ModContent.GetInstance<TEDecraftingAccess>();

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.DecraftingAccess>();

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	}
}
