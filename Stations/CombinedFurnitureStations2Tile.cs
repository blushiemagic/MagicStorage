using System;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedFurnitureStations2Tile : CombinedStationsTile<CombinedFurnitureStations2Item>
	{
		public override Color MapColor => Color.Orange;

		//Combines:
		//(Furniture Tier 1)
		//Bone Welder, Glass Kiln, Honey Dispenser, Ice Machine, Living Loom, Sky Mill, Solidifier
		//(Furniture Tier 2)
		//Decay Chamber, Flesh Cloning Vault, Steampunk Boiler, Lihzahrd Furnace
		public override int[] GetAdjTiles() =>
			new int[]
			{
				Type,
				//Tier 1
				TileID.BoneWelder,
				TileID.GlassKiln,
				TileID.HoneyDispenser,
				TileID.IceMachine,
				TileID.LivingLoom,
				TileID.SkyMill,
				TileID.Solidifier,
				//Tier 2
				TileID.LesionStation,
				TileID.FleshCloningVat,
				TileID.SteampunkBoiler,
				TileID.LihzahrdFurnace
			};

		public override void GetTileDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}
	}
}
