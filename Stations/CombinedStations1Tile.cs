using System;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedStations1Tile : CombinedStationsTile<CombinedStations1Item>
	{
		public override Color MapColor => Color.Orange;

		//Combines:
		//(Tier 1)
		//Work Bench, Furnace, Anvil, Bottle, Sawmill, Sink, Loom, Table
		public override int[] GetAdjTiles() =>
			new int[]
			{
				Type, TileID.WorkBenches, TileID.Furnaces, TileID.Anvils, TileID.Bottles, TileID.Sawmill, TileID.Sinks, TileID.Loom, TileID.Tables, TileID.Tables2
			};

		public override void SafeSetStaticDefaults()
		{
			TileID.Sets.CountsAsWaterSource[Type] = true;
		}

		public override void GetTileDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}
	}
}
