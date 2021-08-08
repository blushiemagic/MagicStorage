using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Stations{
	public class CombinedFurnitureStations1Tile : CombinedStationsTile<CombinedFurnitureStations1Item>{
		//Combines:
		//(Furniture Tier 1)
		//Bone Welder, Glass Kiln, Honey Dispenser, Ice Machine, Living Loom, Sky Mill, Solidifier
		public override int[] GetAdjTiles()
			=> new int[]{
				Type,
				TileID.BoneWelder,
				TileID.GlassKiln,
				TileID.HoneyDispenser,
				TileID.IceMachine,
				TileID.LivingLoom,
				TileID.SkyMill,
				TileID.Solidifier
			};

		public override Color MapColor => Color.Orange;

		public override void GetTileDimensions(out int width, out int height){
			throw new System.NotImplementedException();
		}
	}
}
