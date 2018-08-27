using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MagicStorage.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public class StorageWorld : ModWorld
	{
		private const int saveVersion = 0;
		public static bool kingSlimeDiamond = false;
		public static bool boss1Diamond = false;
		public static bool boss2Diamond = false;
		public static bool boss3Diamond = false;
		public static bool queenBeeDiamond = false;
		public static bool hardmodeDiamond = false;
		public static bool mechBoss1Diamond = false;
		public static bool mechBoss2Diamond = false;
		public static bool mechBoss3Diamond = false;
		public static bool plantBossDiamond = false;
		public static bool golemBossDiamond = false;
		public static bool fishronDiamond = false;
		public static bool ancientCultistDiamond = false;
		public static bool moonlordDiamond = false;
	    public static Dictionary<int, List<int>> TileToCreatingItem = new Dictionary<int, List<int>>();

		public override void Initialize()
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
		}

		public override TagCompound Save()
		{
			TagCompound tag = new TagCompound();
			tag["saveVersion"] = saveVersion;
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
			return tag;
		}
        
	    public override void Load(TagCompound tag)
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

		    Volatile.Write(ref TileToCreatingItem, new Dictionary<int, List<int>>()); // used from threaded RefreshRecipes
		}

	    public override void PostUpdate()
	    {
	        if (TileToCreatingItem.Count == 0)
	        {
	            #region Initialize TileToCreatingItem
                
	            var tileToCreatingItem = Enumerable.Range(0, 10000 + MagicStorage.Instance.ItemType<CraftingAccess>()).Select((x, i) =>
	                {
	                    var item = new Item();
	                    // provide items
	                    try
	                    {
	                        item.SetDefaults(i, true);
	                    }
	                    catch
	                    {
	                        item.SetDefaults();
	                    }

	                    return item;
	                })
	                .Where(x => x.type > 0 && x.createTile >= 0)
	                .Select(x =>
	                {
	                    // provide item and its tiles
	                    var tiles = new List<int> { x.createTile };
	                    if (x.createTile == TileID.GlassKiln || x.createTile == TileID.Hellforge || x.createTile == TileID.AdamantiteForge)
	                    {
	                        tiles.Add(TileID.Furnaces);
	                    }

	                    if (x.createTile == TileID.AdamantiteForge)
	                    {
	                        tiles.Add(TileID.Hellforge);
	                    }

	                    if (x.createTile == TileID.MythrilAnvil)
	                    {
	                        tiles.Add(TileID.Anvils);
	                    }

	                    if (x.createTile == TileID.BewitchingTable || x.createTile == TileID.Tables2)
	                    {
	                        tiles.Add(TileID.Tables);
	                    }

	                    if (x.createTile == TileID.AlchemyTable)
	                    {
	                        tiles.Add(TileID.Bottles);
	                        tiles.Add(TileID.Tables);
	                    }

	                    return new { item = x, tiles = tiles };
	                })
	                // flatten - tile, item
	                .SelectMany(x => x.tiles.Select(t => new { tile = t, x.item }))
	                .GroupBy(x => x.tile)
	                .ToDictionary(x => x.Key, x => x.Select(y => y.item.type).ToList());

	            Volatile.Write(ref TileToCreatingItem, tileToCreatingItem);

	            #endregion
	        }
	    }
	}
}

