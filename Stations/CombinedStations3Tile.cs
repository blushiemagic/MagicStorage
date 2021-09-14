using System;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedStations3Tile : CombinedStationsTile<CombinedStations3Item>
	{
		public override Color MapColor => Color.Orange;

		//Combines:
		//(Tier 1)
		//Work Bench, Furnace, Anvil, Bottle, Sawmill, Sink, Loom, Table
		//(Tier 2)
		//Hellforge, Alchemy Table, Cooking Pot, Tinkerer's Workshop, Dye Vat, Heavy Work Bench, Keg, Teapot
		//(Tier 3)
		//Imbuing Station, Mythril Anvil, Adamantite Forge, Bookcase, Crystal Ball, Blend-O-Matic, Meat Grinder
		public override int[] GetAdjTiles() =>
			new int[]
			{
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
				TileID.TeaKettle,
				//Tier 3
				TileID.ImbuingStation,
				TileID.MythrilAnvil,
				TileID.AdamantiteForge,
				TileID.Bookcases,
				TileID.CrystalBall,
				TileID.Blendomatic,
				TileID.MeatGrinder
			};

		public override void GetTileDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}
	}
}
