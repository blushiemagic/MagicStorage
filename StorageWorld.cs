using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
    public class StorageWorld : ModSystem
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
        //New 1.4 bosses!
        public static bool queenSlimeDiamond = false;
        public static bool empressDiamond = false;

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

        public override TagCompound SaveWorldData()
        {
			TagCompound tag = new TagCompound(){
				["saveVersion"] = saveVersion,
				["kingSlimeDiamond"] = kingSlimeDiamond,
				["boss1Diamond"] = boss1Diamond,
				["boss2Diamond"] = boss2Diamond,
				["boss3Diamond"] = boss3Diamond,
				["queenBeeDiamond"] = queenBeeDiamond,
				["hardmodeDiamond"] = hardmodeDiamond,
				["mechBoss1Diamond"] = mechBoss1Diamond,
				["mechBoss2Diamond"] = mechBoss2Diamond,
				["mechBoss3Diamond"] = mechBoss3Diamond,
				["plantBossDiamond"] = plantBossDiamond,
				["golemBossDiamond"] = golemBossDiamond,
				["fishronDiamond"] = fishronDiamond,
				["ancientCultistDiamond"] = ancientCultistDiamond,
				["moonlordDiamond"] = moonlordDiamond,
                ["queenSlimeDiamond"] = queenSlimeDiamond,
                ["empressDiamond"] = empressDiamond
			};
			return tag;
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
        }
    }
}
