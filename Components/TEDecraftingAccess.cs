using Terraria.ModLoader;
using Terraria;

namespace MagicStorage.Components {
	public class TEDecraftingAccess : TEStorageAccess {
		public override bool ValidTile(in Tile tile) => TileLoader.GetTile(tile.TileType) is DecraftingAccess && tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}
}
