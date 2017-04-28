using System.IO;
using System.Collections.Generic;
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
			fishronDiamond = tag.GetBool("fishronBossDiamond");
			ancientCultistDiamond = tag.GetBool("ancientCultistDiamond");
			moonlordDiamond = tag.GetBool("moonlordDiamond");
		}
	}
}
