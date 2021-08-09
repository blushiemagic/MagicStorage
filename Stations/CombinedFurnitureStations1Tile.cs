using System;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace MagicStorage.Stations
{
	public class CombinedFurnitureStations1Tile : CombinedStationsTile<CombinedFurnitureStations1Item>
	{
		public override Color MapColor => Color.Orange;

		//Combines:
		//(Furniture Tier 1)
		//Bone Welder, Glass Kiln, Honey Dispenser, Ice Machine, Living Loom, Sky Mill, Solidifier
		public override int[] GetAdjTiles() =>
			new int[] { Type, TileID.BoneWelder, TileID.GlassKiln, TileID.HoneyDispenser, TileID.IceMachine, TileID.LivingLoom, TileID.SkyMill, TileID.Solidifier };

		public override void GetTileDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}
	}
}
