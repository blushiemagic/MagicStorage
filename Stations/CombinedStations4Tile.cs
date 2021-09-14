using System;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedStations4Tile : CombinedStationsTile<CombinedStations4Item>
	{
		public override Color MapColor => Color.Orange;

		//Combines:
		//(Tier 1)
		//Work Bench, Furnace, Anvil, Bottle, Sawmill, Sink, Loom, Table
		//(Tier 2)
		//Hellforge, Alchemy Table, Cooking Pot, Tinkerer's Workshop, Dye Vat, Heavy Work Bench, Keg, Teapot
		//(Tier 3)
		//Imbuing Station, Mythril Anvil, Adamantite Forge, Bookcase, Crystal Ball, Blend-O-Matic, Meat Grinder
		//(Furniture Tier 1)
		//Bone Welder, Glass Kiln, Honey Dispenser, Ice Machine, Living Loom, Sky Mill, Solidifier
		//(Furniture Tier 2)
		//Decay Chamber, Flesh Cloning Vault, Steampunk Boiler, Lihzahrd Furnace
		//(Final Tier)
		//Autohammer, Ancient Manipulator, All Liquids, Tombstone (Ecto Mist), Campfire, Demon/Crimson Altar
		public override int[] GetAdjTiles() =>
			new int[]
			{
				Type,
				//Tier 1 (Standard)
				TileID.WorkBenches,
				TileID.Furnaces,
				TileID.Anvils,
				TileID.Bottles,
				TileID.Sawmill,
				TileID.Sinks,
				TileID.Loom,
				TileID.Tables,
				TileID.Tables2,
				//Tier 2 (Standard)
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
				TileID.MeatGrinder,
				//Tier 1 (Furniture)
				TileID.BoneWelder,
				TileID.GlassKiln,
				TileID.HoneyDispenser,
				TileID.IceMachine,
				TileID.LivingLoom,
				TileID.SkyMill,
				TileID.Solidifier,
				//Tier 2 (Furniture)
				TileID.LesionStation,
				TileID.FleshCloningVat,
				TileID.SteampunkBoiler,
				TileID.LihzahrdFurnace,
				//Final Tier
				TileID.CrystalBall,
				TileID.Autohammer,
				TileID.LunarCraftingStation,
				TileID.Campfire,
				TileID.DemonAltar
			};

		public override void SafeSetStaticDefaults()
		{
			TileID.Sets.CountsAsWaterSource[Type] = true;
			TileID.Sets.CountsAsLavaSource[Type] = true;
			TileID.Sets.CountsAsHoneySource[Type] = true;
		}

		public override void GetTileDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}
	}
}
