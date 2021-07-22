using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using MagicStorage.Items;

namespace MagicStorage.NPCs
{
	public class ShadowDiamondDrop : GlobalNPC
	{
		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
		{
			if (npc.type == NPCID.KingSlime)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.EyeofCthulhu)
				DropDiamond(Main.expertMode ? 2 : 1, npcLoot);
			else if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.BrainofCthulhu)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.SkeletronHead)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.QueenBee)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.WallofFlesh)
				DropDiamond(2, npcLoot);
			else if (npc.type == NPCID.TheDestroyer)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.SkeletronPrime)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.Plantera)
				DropDiamond(Main.expertMode ? 2 : 1, npcLoot);
			else if (npc.type == NPCID.Golem)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.DukeFishron)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.CultistBoss)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.MoonLordCore)
				DropDiamond(Main.expertMode ? 3 : 2, npcLoot);
			else if (npc.type == NPCID.QueenSlimeBoss)
				DropDiamond(1, npcLoot);
			else if (npc.type == NPCID.HallowBoss)
				DropDiamond(1, npcLoot);
		}

		public override void OnKill(NPC npc){
			//Set the flags here instead of ModifyNPCLoot in order to let loot happen properly
			if (npc.type == NPCID.KingSlime)
				NPC.SetEventFlagCleared(ref StorageWorld.kingSlimeDiamond, -1);
			else if (npc.type == NPCID.EyeofCthulhu)
				NPC.SetEventFlagCleared(ref StorageWorld.boss1Diamond, -1);
			else if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.BrainofCthulhu)
				NPC.SetEventFlagCleared(ref StorageWorld.boss2Diamond, -1);
			else if (npc.type == NPCID.SkeletronHead)
				NPC.SetEventFlagCleared(ref StorageWorld.boss3Diamond, -1);
			else if (npc.type == NPCID.QueenBee)
				NPC.SetEventFlagCleared(ref StorageWorld.queenBeeDiamond, -1);
			else if (npc.type == NPCID.WallofFlesh)
				NPC.SetEventFlagCleared(ref StorageWorld.hardmodeDiamond, -1);
			else if (npc.type == NPCID.TheDestroyer)
				NPC.SetEventFlagCleared(ref StorageWorld.mechBoss1Diamond, -1);
			else if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
				NPC.SetEventFlagCleared(ref StorageWorld.mechBoss2Diamond, -1);
			else if (npc.type == NPCID.SkeletronPrime)
				NPC.SetEventFlagCleared(ref StorageWorld.mechBoss3Diamond, -1);
			else if (npc.type == NPCID.Plantera)
				NPC.SetEventFlagCleared(ref StorageWorld.plantBossDiamond, -1);
			else if (npc.type == NPCID.Golem)
				NPC.SetEventFlagCleared(ref StorageWorld.golemBossDiamond, -1);
			else if (npc.type == NPCID.DukeFishron)
				NPC.SetEventFlagCleared(ref StorageWorld.fishronDiamond, -1);
			else if (npc.type == NPCID.CultistBoss)
				NPC.SetEventFlagCleared(ref StorageWorld.ancientCultistDiamond, -1);
			else if (npc.type == NPCID.MoonLordCore)
				NPC.SetEventFlagCleared(ref StorageWorld.moonlordDiamond, -1);
			else if (npc.type == NPCID.QueenSlimeBoss)
				NPC.SetEventFlagCleared(ref StorageWorld.queenSlimeDiamond, -1);
			else if (npc.type == NPCID.HallowBoss)
				NPC.SetEventFlagCleared(ref StorageWorld.empressDiamond, -1);
		}

		private static void DropDiamond(int stack, NPCLoot npcLoot)
		{
			var rule = new LeadingConditionRule(new ShadowDiamondCondition());

			//Guaranteed chance to drop 1 item, if the condition succeeds
			rule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShadowDiamond>(), minimumDropped: stack, maximumDropped: stack));

			npcLoot.Add(rule);
		}
	}

	internal class ShadowDiamondCondition : IItemDropRuleCondition {
		public bool CanDrop(DropAttemptInfo info)
			=> !info.IsInSimulation && info.npc.type switch{
				NPCID.KingSlime => !StorageWorld.kingSlimeDiamond,
				NPCID.EyeofCthulhu => !StorageWorld.boss1Diamond,
				NPCID.EaterofWorldsHead => info.npc.boss && !StorageWorld.boss2Diamond,
				NPCID.EaterofWorldsBody => info.npc.boss && !StorageWorld.boss2Diamond,
				NPCID.EaterofWorldsTail => info.npc.boss && !StorageWorld.boss2Diamond,
				NPCID.BrainofCthulhu => !StorageWorld.boss2Diamond,
				NPCID.SkeletronHead => !StorageWorld.boss3Diamond,
				NPCID.QueenBee => !StorageWorld.queenBeeDiamond,
				NPCID.WallofFlesh => !StorageWorld.hardmodeDiamond,
				NPCID.TheDestroyer => !StorageWorld.mechBoss1Diamond,
				NPCID.Retinazer => !StorageWorld.mechBoss2Diamond,
				NPCID.Spazmatism => !StorageWorld.mechBoss2Diamond,
				NPCID.SkeletronPrime => !StorageWorld.mechBoss3Diamond,
				NPCID.Plantera => !StorageWorld.plantBossDiamond,
				NPCID.Golem => !StorageWorld.golemBossDiamond,
				NPCID.DukeFishron => !StorageWorld.fishronDiamond,
				NPCID.CultistBoss => !StorageWorld.ancientCultistDiamond,
				NPCID.MoonLordCore => !StorageWorld.moonlordDiamond,
				NPCID.QueenSlimeBoss => !StorageWorld.queenSlimeDiamond,
				NPCID.HallowBoss => !StorageWorld.empressDiamond,
				_ => false  //Default to false to shove everything else under the rug
			};

		public bool CanShowItemDropInUI() => true;  //Don't make the item show up in the bestiary

		public string GetConditionDescription()
			=> "Dropped on first kill of every vanilla boss";
	}
}