using MagicStorage.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		public static void AddCraftingZones(Item item, ref CraftingInformation information) {
			if (item.IsAir)
				return;
			
			if (item.createTile >= TileID.Dirt) {
				information.adjTiles[item.createTile] = true;
				switch (item.createTile) {
					case TileID.GlassKiln:
					case TileID.Hellforge:
						information.adjTiles[TileID.Furnaces] = true;
						break;
					case TileID.AdamantiteForge:
						information.adjTiles[TileID.Furnaces] = true;
						information.adjTiles[TileID.Hellforge] = true;
						break;
					case TileID.MythrilAnvil:
						information.adjTiles[TileID.Anvils] = true;
						break;
					case TileID.BewitchingTable:
					case TileID.Tables2:
						information.adjTiles[TileID.Tables] = true;
						goto case TileID.Tables;
					case TileID.AlchemyTable:
						information.adjTiles[TileID.Bottles] = true;
						information.adjTiles[TileID.Tables] = true;
						break;
					case TileID.Tombstones:
						information.graveyard = true;
						break;
					case TileID.WorkBenches:
					case TileID.Tables:
						information.adjTiles[TileID.Chairs] = true;
						break;
				}

				// Briefly swap out the array with the information's array
				Player player = Main.LocalPlayer;
				bool[] oldAdjTile = player.adjTile;
				player.adjTile = information.adjTiles;

				TileLoader.AdjTiles(player, item.createTile);

				player.adjTile = oldAdjTile;

				if (TileID.Sets.CountsAsWaterSource[item.createTile])
					information.water = true;
				if (TileID.Sets.CountsAsLavaSource[item.createTile])
					information.lava = true;
				if (TileID.Sets.CountsAsHoneySource[item.createTile])
					information.honey = true;
				if (TileID.Sets.Campfire[item.createTile])
					information.campfire = true;
			}

			switch (item.type) {
				case ItemID.WaterBucket:
				case ItemID.BottomlessBucket:
					information.water = true;
					break;
				case ItemID.LavaBucket:
				case ItemID.BottomlessLavaBucket:
					information.lava = true;
					break;
				case ItemID.HoneyBucket:
				case ItemID.BottomlessHoneyBucket:
					information.honey = true;
					break;
				case ItemID.BottomlessShimmerBucket:
					information.shimmer = true;
					break;
			}

			if (item.type == ModContent.ItemType<SnowBiomeEmulator>())
				information.snow = true;

			if (item.type == ModContent.ItemType<BiomeGlobe>()) {
				information.snow = true;
				information.graveyard = true;
				information.campfire = true;
				information.water = true;
				information.lava = true;
				information.honey = true;

				information.adjTiles[TileID.DemonAltar] = true;
			}

			if (information.graveyard)
				information.adjTiles[TileID.Tombstones] = true;
			if (information.campfire)
				information.adjTiles[TileID.Campfire] = true;
		}
	}
}
