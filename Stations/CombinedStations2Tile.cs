using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class CombinedStations2Tile : CombinedStationsTile<CombinedStations2Item>
	{
		public override Color MapColor => Color.Orange;

		//Combines:
		//(Tier 1)
		//Work Bench, Furnace, Anvil, Bottle, Sawmill, Sink, Loom, Table
		//(Tier 2)
		//Hellforge, Alchemy Table, Cooking Pot, Tinkerer's Workshop, Dye Vat, Heavy Work Bench, Keg, Teapot
		public override int[] GetAdjTiles()
			=> new int[]{
				Type,
				//Tier 1
				TileID.WorkBenches,
				TileID.Furnaces,
				TileID.Anvils,
				TileID.Bottles,
				TileID.Sawmill,
				TileID.Sinks,
				TileID.Loom,
				TileID.Tables,
				TileID.Tables2,
				//Tier 2
				TileID.Hellforge,
				TileID.AlchemyTable,
				TileID.CookingPots,
				TileID.TinkerersWorkbench,
				TileID.DyeVat,
				TileID.HeavyWorkBench,
				TileID.Kegs,
				TileID.TeaKettle
			};

		public override void SafeSetStaticDefaults()
		{
			TileID.Sets.CountsAsWaterSource[Type] = true;
		}

		public override void GetTileDimensions(out int width, out int height)
		{
			width = 3;
			height = 3;
		}
	}
}
