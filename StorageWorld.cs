using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public class StorageWorld : ModSystem
	{
		private const int SaveVersion = 0;
		public static bool kingSlimeDiamond;
		public static bool boss1Diamond;
		public static bool boss2Diamond;
		public static bool boss3Diamond;
		public static bool queenBeeDiamond;
		public static bool hardmodeDiamond;
		public static bool mechBoss1Diamond;
		public static bool mechBoss2Diamond;
		public static bool mechBoss3Diamond;
		public static bool plantBossDiamond;
		public static bool golemBossDiamond;
		public static bool fishronDiamond;
		public static bool ancientCultistDiamond;
		public static bool moonlordDiamond;

		//New 1.4 bosses!
		public static bool queenSlimeDiamond;
		public static bool empressDiamond;

		public static Dictionary<int, List<int>> TileToCreatingItem = new();

		public override void OnWorldLoad()
		{
			kingSlimeDiamond = false;
			boss1Diamond = false;
			boss2Diamond = false;
			boss3Diamond = false;
			queenBeeDiamond = false;
			hardmodeDiamond = false;
			mechBoss1Diamond = false;
			mechBoss2Diamond = false;
			mechBoss3Diamond = false;
			plantBossDiamond = false;
			golemBossDiamond = false;
			fishronDiamond = false;
			ancientCultistDiamond = false;
			moonlordDiamond = false;
			queenSlimeDiamond = false;
			empressDiamond = false;
		}

		public override void SaveWorldData(TagCompound tag)
		{
			tag["saveVersion"] = SaveVersion;
			tag["kingSlimeDiamond"] = kingSlimeDiamond;
			tag["boss1Diamond"] = boss1Diamond;
			tag["boss2Diamond"] = boss2Diamond;
			tag["boss3Diamond"] = boss3Diamond;
			tag["queenBeeDiamond"] = queenBeeDiamond;
			tag["hardmodeDiamond"] = hardmodeDiamond;
			tag["mechBoss1Diamond"] = mechBoss1Diamond;
			tag["mechBoss2Diamond"] = mechBoss2Diamond;
			tag["mechBoss3Diamond"] = mechBoss3Diamond;
			tag["plantBossDiamond"] = plantBossDiamond;
			tag["golemBossDiamond"] = golemBossDiamond;
			tag["fishronDiamond"] = fishronDiamond;
			tag["ancientCultistDiamond"] = ancientCultistDiamond;
			tag["moonlordDiamond"] = moonlordDiamond;
			tag["queenSlimeDiamond"] = queenSlimeDiamond;
			tag["empressDiamond"] = empressDiamond;
		}

		public override void LoadWorldData(TagCompound tag)
		{
			kingSlimeDiamond = tag.GetBool("kingSlimeDiamond");
			boss1Diamond = tag.GetBool("boss1Diamond");
			boss2Diamond = tag.GetBool("boss2Diamond");
			boss3Diamond = tag.GetBool("boss3Diamond");
			queenBeeDiamond = tag.GetBool("queenBeeDiamond");
			hardmodeDiamond = tag.GetBool("hardmodeDiamond");
			mechBoss1Diamond = tag.GetBool("mechBoss1Diamond");
			mechBoss2Diamond = tag.GetBool("mechBoss2Diamond");
			mechBoss3Diamond = tag.GetBool("mechBoss3Diamond");
			plantBossDiamond = tag.GetBool("plantBossDiamond");
			golemBossDiamond = tag.GetBool("golemBossDiamond");
			fishronDiamond = tag.GetBool("fishronDiamond");
			ancientCultistDiamond = tag.GetBool("ancientCultistDiamond");
			moonlordDiamond = tag.GetBool("moonlordDiamond");
			queenSlimeDiamond = tag.GetBool("queenSlimeDiamond");
			empressDiamond = tag.GetBool("empressDiamond");

			Volatile.Write(ref TileToCreatingItem, new Dictionary<int, List<int>>()); // used from threaded RefreshRecipes
		}

		public override void PostUpdateWorld()
		{
			if (TileToCreatingItem.Count != 0)
				return;

			#region Initialize TileToCreatingItem

			Dictionary<int, List<int>> tileToCreatingItem = Enumerable.Range(0, ItemLoader.ItemCount)
				.Select((_, i) =>
				{
					Item item = new();
					// provide items
					try
					{
						item.SetDefaults(i, true);
					}
					catch
					{
						return null;
					}

					return item;
				})
				.Where(item => item is not null)
				.Where(item => item.type > ItemID.None && item.createTile >= TileID.Dirt)
				.Select(item =>
				{
					// provide item and its tiles
					List<int> tiles = new() { item.createTile };
					switch (item.createTile)
					{
						case TileID.GlassKiln:
						case TileID.Hellforge:
							tiles.Add(TileID.Furnaces);
							break;
						case TileID.AdamantiteForge:
							tiles.Add(TileID.Furnaces);
							tiles.Add(TileID.Hellforge);
							break;
						case TileID.MythrilAnvil:
							tiles.Add(TileID.Anvils);
							break;
						case TileID.BewitchingTable:
						case TileID.Tables2:
							tiles.Add(TileID.Tables);
							break;
						case TileID.AlchemyTable:
							tiles.Add(TileID.Bottles);
							tiles.Add(TileID.Tables);
							break;
					}

					return (item, tiles);
				})
				// flatten - tile, item
				.SelectMany(x => x.tiles.Select(tile => (tile, x.item)))
				.GroupBy(x => x.tile)
				.ToDictionary(x => x.Key, x => x.Select(y => y.item.type).ToList());

			Volatile.Write(ref TileToCreatingItem, tileToCreatingItem);

			#endregion
		}
	}
}
