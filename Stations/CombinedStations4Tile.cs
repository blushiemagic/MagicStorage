using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class CombinedStations4Tile : CombinedStationsTile<CombinedStations4Item>
	{
		public override Color MapColor => Color.Orange;

		//Combines:
		//(Tier 1)
		//Work Bench, Furnace, Anvil, Bottle, Sawmill, Sink, Loom, Table
		//(Tier 2)
		//Hellforge, Alchemy Table, Cooking Pot, Tinkerer's Workshop, Dye Vat, Heavy Work Bench, Keg, Teapot
		//(Tier 3)
		//Imbuing Station, Mythril Anvil, Adamantite Forge, Bookcase, Crystal Ball, Blend-O-Matic
		//(Furniture Tier 1)
		//Bone Welder, Glass Kiln, Honey Dispenser, Ice Machine, Living Loom, Sky Mill, Solidifier
		//(Furniture Tier 2)
		//Meat Grinder, Decay Chamber, Flesh Cloning Vault, Steampunk Boiler, Lihzahrd Furnace
		//(Final Tier)
		//Autohammer, Ancient Manipulator, All Liquids
		public override int[] GetAdjTiles() {
			var tiles = GetStandardTiles();

			if (ModLoader.TryGetMod("Fargowiltas", out Mod Fargowiltas)) {
				// Cross mod compatibility: Golden Dipping Vat and Crucible of the Cosmos
				var vat = Fargowiltas.Find<ModTile>("GoldenDippingVatSheet");
				tiles.Add(vat.Type);
				tiles.AddRange(vat.AdjTiles);

				var crucible = Fargowiltas.Find<ModTile>("CrucibleCosmosSheet");
				tiles.Add(crucible.Type);
				tiles.AddRange(crucible.AdjTiles);
			}

			return tiles.Distinct().ToArray();
		}

		private List<int> GetStandardTiles() {
			return new ()
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
				//Tier 1 (Furniture)
				TileID.BoneWelder,
				TileID.GlassKiln,
				TileID.HoneyDispenser,
				TileID.IceMachine,
				TileID.LivingLoom,
				TileID.SkyMill,
				TileID.Solidifier,
				//Tier 2 (Furniture)
				TileID.MeatGrinder,
				TileID.LesionStation,
				TileID.FleshCloningVat,
				TileID.SteampunkBoiler,
				TileID.LihzahrdFurnace,
				//Final Tier
				TileID.Autohammer,
				TileID.LunarCraftingStation
			};
		}

		public override void SafeSetStaticDefaults()
		{
			TileID.Sets.CountsAsWaterSource[Type] = true;
			TileID.Sets.CountsAsLavaSource[Type] = true;
			TileID.Sets.CountsAsHoneySource[Type] = true;
		}

		public override void GetTileDimensions(out int width, out int height)
		{
			width = 3;
			height = 3;
		}
	}
}
