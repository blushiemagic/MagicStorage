using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public class EnvironmentAccess : StorageAccess {
		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.EnvironmentAccess>();

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		// TODO: GUI for selecting modules
		public override bool RightClick(int i, int j) => false;
	}
}
