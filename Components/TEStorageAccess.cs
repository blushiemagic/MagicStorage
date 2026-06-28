using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public class TEStorageAccess : TEStorageComponent {
		public override bool ValidTile(in Tile tile) => TileLoader.GetTile(tile.TileType) is StorageAccess && tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}
}
