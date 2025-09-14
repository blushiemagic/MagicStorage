using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		public static void SetVanillaAdjTiles(Item item, out bool hasSnow, out bool hasGraveyard) {
			hasSnow = false;
			hasGraveyard = false;
			
			Player player = Main.LocalPlayer;
			bool[] adjTiles = player.adjTile;
			if (item.createTile >= TileID.Dirt) {
				adjTiles[item.createTile] = true;
				switch (item.createTile) {
					case TileID.GlassKiln:
					case TileID.Hellforge:
						adjTiles[TileID.Furnaces] = true;
						break;
					case TileID.AdamantiteForge:
						adjTiles[TileID.Furnaces] = true;
						adjTiles[TileID.Hellforge] = true;
						break;
					case TileID.MythrilAnvil:
						adjTiles[TileID.Anvils] = true;
						break;
					case TileID.BewitchingTable:
					case TileID.Tables2:
						adjTiles[TileID.Tables] = true;
						break;
					case TileID.AlchemyTable:
						adjTiles[TileID.Bottles] = true;
						adjTiles[TileID.Tables] = true;
						break;
					case TileID.Tombstones:
						hasGraveyard = true;
						break;
				}

				switch (item.createTile) {
					case TileID.WorkBenches:
					case TileID.Tables:
					case TileID.Tables2:
						adjTiles[TileID.Chairs] = true;
						break;
				}

				TileLoader.AdjTiles(Main.LocalPlayer, item.createTile);

				if (TileID.Sets.CountsAsWaterSource[item.createTile])
					player.adjWater = true;
				if (TileID.Sets.CountsAsLavaSource[item.createTile])
					player.adjLava = true;
				if (TileID.Sets.CountsAsHoneySource[item.createTile])
					player.adjHoney = true;
				if (player.adjTile[TileID.Tombstones])
					hasGraveyard = true;
			}

			int globeItem = ModContent.ItemType<Items.BiomeGlobe>();

			if (item.type == ItemID.WaterBucket || item.type == ItemID.BottomlessBucket || item.type == globeItem)
				player.adjWater = true;
			if (item.type == ItemID.LavaBucket || item.type == ItemID.BottomlessLavaBucket || item.type == globeItem)
				player.adjLava = true;
			if (item.type == ItemID.HoneyBucket || item.type == ItemID.BottomlessHoneyBucket || item.type == globeItem)
				player.adjHoney = true;
			if (item.type == ItemID.BottomlessShimmerBucket)
				player.adjShimmer = true;
			if (item.type == ModContent.ItemType<Items.SnowBiomeEmulator>() || item.type == globeItem)
				hasSnow = true;
			if (item.type == globeItem) {
				adjTiles[TileID.Campfire] = true;
				adjTiles[TileID.DemonAltar] = true;
				hasGraveyard = true;
			}
		}
	}
}
