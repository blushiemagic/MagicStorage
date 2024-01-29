using Terraria.ModLoader;
using Terraria;

namespace MagicStorage.Components {
	public class TEDecraftingAccess : TEStorageAccess {
		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<DecraftingAccess>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}
}
