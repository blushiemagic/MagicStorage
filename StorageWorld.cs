using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;
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

		//Modded support
		public static HashSet<string> moddedDiamonds;

		internal static HashSet<int> disallowDropModded;
		internal static Dictionary<int, Func<int>> moddedDiamondsDroppedByType;
		internal static Dictionary<int, IItemDropRule> moddedDiamondDropRulesByType;

		public override void Load() {
			disallowDropModded = new();
			moddedDiamondDropRulesByType = new();
		}

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

			moddedDiamonds = new();
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
			tag["modded"] = moddedDiamonds.ToList();
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

			if (tag.GetList<string>("modded") is List<string> list)
				moddedDiamonds = new(list);
		}
	}
}
