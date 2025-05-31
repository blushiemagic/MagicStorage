using MagicStorage.Common.Systems;
using MagicStorage.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Global {
	internal class GolemTextTracking : GlobalNPC {
		public override void OnKill(NPC npc) {
			bool killedMoonLord = false;
			bool killedAllMechs = false;

			if (npc.type == NPCID.MoonLordCore && !NPC.downedMoonlord)
				killedMoonLord = true;
			else if (npc.type == NPCID.Retinazer && !NPC.AnyNPCs(NPCID.Spazmatism) && NPC.downedMechBoss1 && !NPC.downedMechBoss2 && NPC.downedMechBoss3)
				killedAllMechs = true;
			else if (npc.type == NPCID.Spazmatism && !NPC.AnyNPCs(NPCID.Retinazer) && NPC.downedMechBoss1 && !NPC.downedMechBoss2 && NPC.downedMechBoss3)
				killedAllMechs = true;
			else if (npc.type == NPCID.TheDestroyer && !NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
				killedAllMechs = true;
			else if (npc.type == NPCID.SkeletronPrime && NPC.downedMechBoss1 && NPC.downedMechBoss2 && !NPC.downedMechBoss3)
				killedAllMechs = true;

			if (killedAllMechs || killedMoonLord) {
				if (killedAllMechs)
					StorageWorld.UnlockedHelpTips.PortableAccess_AdvancedTier = true;
				if (killedMoonLord)
					StorageWorld.UnlockedHelpTips.PortableAccess_UltimateTier = true;

				Golem.ReportNewTipUnlocked();
			}
		}
	}
}
