using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.Systems
{
	public class StorageWorld : ModSystem
	{
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
		public static HashSet<int> moddedDiamonds;
		private static HashSet<string> unloadedModdedDiamonds;

		internal static HashSet<int> disallowDropModded;
		internal static Dictionary<int, Func<int>> moddedDiamondsDroppedByType;
		internal static Dictionary<int, IItemDropRule> moddedDiamondDropRulesByType;

		public override void Load()
		{
			disallowDropModded = new();
			moddedDiamondsDroppedByType = new();
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
			unloadedModdedDiamonds = new();
		}

		public override void PreSaveAndQuit() {
			StoragePlayer.LocalPlayer.CloseStorage();
		}

		public override void PreWorldGen() => OnWorldLoad();

		public override void SaveWorldData(TagCompound tag)
		{
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
			tag["modded"] = moddedDiamonds.Select(i => ModContent.GetModNPC(i)).Where(m => m is not null).Select(m => $"{m.Mod.Name}:{m.Name}").Concat(unloadedModdedDiamonds).ToList();

			if (!Main.dedServ)
				MagicStorageMod.Instance.optionsConfig.Save();
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
			{
				foreach (string identifier in list)
				{
					string[] split = identifier.Split(':');

					if (split.Length != 2)
						continue;

					if (ModLoader.TryGetMod(split[0], out Mod source) && source.TryFind(split[1], out ModNPC npc))
						moddedDiamonds.Add(npc.Type);
					else
						unloadedModdedDiamonds.Add(identifier);
				}
			}
		}

		public override void NetSend(BinaryWriter writer)
		{
			BitsByte bb = new(kingSlimeDiamond, boss1Diamond, boss2Diamond, boss3Diamond, queenBeeDiamond, hardmodeDiamond, mechBoss1Diamond, mechBoss2Diamond);
			writer.Write(bb);

			bb = new(mechBoss3Diamond, plantBossDiamond, golemBossDiamond, fishronDiamond, ancientCultistDiamond, moonlordDiamond, queenSlimeDiamond, empressDiamond);
			writer.Write(bb);

			writer.Write((ushort)moddedDiamonds.Count);
			foreach (int modded in moddedDiamonds)
				writer.Write(modded);
		}

		public override void NetReceive(BinaryReader reader)
		{
			BitsByte bb = reader.ReadByte();
			bb.Retrieve(ref kingSlimeDiamond, ref boss1Diamond, ref boss2Diamond, ref boss3Diamond, ref queenBeeDiamond, ref hardmodeDiamond, ref mechBoss1Diamond, ref mechBoss2Diamond);

			bb = reader.ReadByte();
			bb.Retrieve(ref mechBoss3Diamond, ref plantBossDiamond, ref golemBossDiamond, ref fishronDiamond, ref ancientCultistDiamond, ref moonlordDiamond, ref queenSlimeDiamond, ref empressDiamond);

			moddedDiamonds.Clear();
			ushort moddedCount = reader.ReadUInt16();

			for (int i = 0; i < moddedCount; i++)
				moddedDiamonds.Add(reader.ReadInt32());
		}
	}
}
