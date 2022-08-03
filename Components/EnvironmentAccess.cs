using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public class EnvironmentAccess : StorageAccess {
		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.EnvironmentAccess>();

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public override ModTileEntity GetTileEntity() => ModContent.GetInstance<TEEnvironmentAccess>();

		public override bool RightClick(int i, int j) {
			bool ret = base.RightClick(i, j);

			Tile tile = Main.tile[i, j];
			Point16 topLeft = new(i - tile.TileFrameX / 18, j - tile.TileFrameY / 18);

			if (TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity entity) && entity is TEEnvironmentAccess access && StoragePlayer.IsStorageEnvironment()) {
				EnvironmentGUI.LoadModules(access);

				/*
				Main.NewTextMultiline("Opened Environment Simulator -- Modules: " + access.Count + "\n" +
					"  " + string.Join("\n  ", access.Modules.Select(m => m.FullName)),
					c: Color.White);
				*/
			}

			return ret;
		}
	}
}
