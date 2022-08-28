using MagicStorage.NPCs;
using Microsoft.Xna.Framework;
using System.Linq;
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
					SetPendingText();
				else
					NetHelper.SendGolemTextUpdate();
			}
		}

		internal static void SetPendingText() {
			foreach (Golem golem in Main.npc.Take(Main.maxNPCs).Where(n => n.active && n.ModNPC is Golem).Select(n => n.ModNPC as Golem))
				golem.pendingNewHelpTextCheck = true;

			// TODO: localization entry
			Main.NewText("The Automaton has new help tips available.", Color.CadetBlue);
		}
	}
}
