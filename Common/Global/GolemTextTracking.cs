using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Global {
	internal class GolemTextTracking : GlobalNPC {
		public override void OnKill(NPC npc) {
			bool newText = false;

			if (npc.type == NPCID.MoonLordCore && !NPC.downedMoonlord)
				newText = true;
			else if (npc.type == NPCID.Retinazer && !NPC.AnyNPCs(NPCID.Spazmatism) && NPC.downedMechBoss1 && !NPC.downedMechBoss2 && NPC.downedMechBoss3)
				newText = true;
			else if (npc.type == NPCID.Spazmatism && !NPC.AnyNPCs(NPCID.Retinazer) && NPC.downedMechBoss1 && !NPC.downedMechBoss2 && NPC.downedMechBoss3)
				newText = true;
			else if (npc.type == NPCID.TheDestroyer && !NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
				newText = true;
			else if (npc.type == NPCID.SkeletronPrime && NPC.downedMechBoss1 && NPC.downedMechBoss2 && !NPC.downedMechBoss3)
				newText = true;

			if (newText) {
				if (Main.netMode == NetmodeID.SinglePlayer)
					SayPendingText();
				else
					NetHelper.SendGolemTextUpdate();
			}
		}

		public static void SayPendingText() {
			// Legacy code, decided later that it's fine if the NPC checks every tick for the tip flags
			/*
			foreach (Golem golem in Main.npc.Take(Main.maxNPCs).Where(n => n.active && n.ModNPC is Golem).Select(n => n.ModNPC as Golem))
				golem.pendingNewHelpTextCheck = true;
			*/

			Main.NewText(MagicStorageMod.Instance.GetLocalization("Dialogue.NewHelpAvailable").Value, Color.CadetBlue);
		}
	}
}
