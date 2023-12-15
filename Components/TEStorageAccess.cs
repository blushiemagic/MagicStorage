using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public class TEStorageAccess : TEStorageComponent {
		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<StorageAccess>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}
}
