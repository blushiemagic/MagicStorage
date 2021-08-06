using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class ShadowDiamondDrop : GlobalNPC
	{
		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
		{
			switch (npc.type)
			{
				case NPCID.KingSlime:
					DropDiamond(1, npcLoot);
					break;
				case NPCID.EyeofCthulhu:
					DropDiamond(Main.expertMode ? 2 : 1, npcLoot);
					break;
				case NPCID.EaterofWorldsHead:
				case NPCID.EaterofWorldsBody:
				case NPCID.EaterofWorldsTail:
				case NPCID.BrainofCthulhu:
				case NPCID.SkeletronHead:
				case NPCID.QueenBee:
					DropDiamond(1, npcLoot);
					break;
				case NPCID.WallofFlesh:
					DropDiamond(2, npcLoot);
					break;
				case NPCID.TheDestroyer:
				case NPCID.Retinazer:
				case NPCID.Spazmatism:
				case NPCID.SkeletronPrime:
					DropDiamond(1, npcLoot);
					break;
				case NPCID.Plantera:
					DropDiamond(Main.expertMode ? 2 : 1, npcLoot);
					break;
				case NPCID.Golem:
				case NPCID.DukeFishron:
				case NPCID.CultistBoss:
					DropDiamond(1, npcLoot);
					break;
				case NPCID.MoonLordCore:
					DropDiamond(Main.expertMode ? 3 : 2, npcLoot);
					break;
				case NPCID.QueenSlimeBoss:
				case NPCID.HallowBoss:
					DropDiamond(1, npcLoot);
					break;
			}
		}

		public override void OnKill(NPC npc)
		{
			switch (npc.type)
			{
				//Set the flags here instead of ModifyNPCLoot in order to let loot happen properly
				case NPCID.KingSlime:
					NPC.SetEventFlagCleared(ref StorageWorld.kingSlimeDiamond, -1);
					break;
				case NPCID.EyeofCthulhu:
					NPC.SetEventFlagCleared(ref StorageWorld.boss1Diamond, -1);
					break;
				case NPCID.EaterofWorldsHead:
				case NPCID.EaterofWorldsBody:
				case NPCID.EaterofWorldsTail:
				case NPCID.BrainofCthulhu:
					NPC.SetEventFlagCleared(ref StorageWorld.boss2Diamond, -1);
					break;
				case NPCID.SkeletronHead:
					NPC.SetEventFlagCleared(ref StorageWorld.boss3Diamond, -1);
					break;
				case NPCID.QueenBee:
					NPC.SetEventFlagCleared(ref StorageWorld.queenBeeDiamond, -1);
					break;
				case NPCID.WallofFlesh:
					NPC.SetEventFlagCleared(ref StorageWorld.hardmodeDiamond, -1);
					break;
				case NPCID.TheDestroyer:
					NPC.SetEventFlagCleared(ref StorageWorld.mechBoss1Diamond, -1);
					break;
				case NPCID.Retinazer:
				case NPCID.Spazmatism:
					NPC.SetEventFlagCleared(ref StorageWorld.mechBoss2Diamond, -1);
					break;
				case NPCID.SkeletronPrime:
					NPC.SetEventFlagCleared(ref StorageWorld.mechBoss3Diamond, -1);
					break;
				case NPCID.Plantera:
					NPC.SetEventFlagCleared(ref StorageWorld.plantBossDiamond, -1);
					break;
				case NPCID.Golem:
					NPC.SetEventFlagCleared(ref StorageWorld.golemBossDiamond, -1);
					break;
				case NPCID.DukeFishron:
					NPC.SetEventFlagCleared(ref StorageWorld.fishronDiamond, -1);
					break;
				case NPCID.CultistBoss:
					NPC.SetEventFlagCleared(ref StorageWorld.ancientCultistDiamond, -1);
					break;
				case NPCID.MoonLordCore:
					NPC.SetEventFlagCleared(ref StorageWorld.moonlordDiamond, -1);
					break;
				case NPCID.QueenSlimeBoss:
					NPC.SetEventFlagCleared(ref StorageWorld.queenSlimeDiamond, -1);
					break;
				case NPCID.HallowBoss:
					NPC.SetEventFlagCleared(ref StorageWorld.empressDiamond, -1);
					break;
			}
		}

		private static void DropDiamond(int stack, NPCLoot npcLoot)
		{
			LeadingConditionRule rule = new(new ShadowDiamondCondition());

			//Guaranteed chance to drop 1 item, if the condition succeeds
			rule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShadowDiamond>(), minimumDropped: stack, maximumDropped: stack));

			npcLoot.Add(rule);
		}
	}

	internal class ShadowDiamondCondition : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) =>
			!info.IsInSimulation && info.npc.type switch
			{
				NPCID.KingSlime         => !StorageWorld.kingSlimeDiamond,
				NPCID.EyeofCthulhu      => !StorageWorld.boss1Diamond,
				NPCID.EaterofWorldsHead => info.npc.boss && !StorageWorld.boss2Diamond,
				NPCID.EaterofWorldsBody => info.npc.boss && !StorageWorld.boss2Diamond,
				NPCID.EaterofWorldsTail => info.npc.boss && !StorageWorld.boss2Diamond,
				NPCID.BrainofCthulhu    => !StorageWorld.boss2Diamond,
				NPCID.SkeletronHead     => !StorageWorld.boss3Diamond,
				NPCID.QueenBee          => !StorageWorld.queenBeeDiamond,
				NPCID.WallofFlesh       => !StorageWorld.hardmodeDiamond,
				NPCID.TheDestroyer      => !StorageWorld.mechBoss1Diamond,
				NPCID.Retinazer         => !StorageWorld.mechBoss2Diamond,
				NPCID.Spazmatism        => !StorageWorld.mechBoss2Diamond,
				NPCID.SkeletronPrime    => !StorageWorld.mechBoss3Diamond,
				NPCID.Plantera          => !StorageWorld.plantBossDiamond,
				NPCID.Golem             => !StorageWorld.golemBossDiamond,
				NPCID.DukeFishron       => !StorageWorld.fishronDiamond,
				NPCID.CultistBoss       => !StorageWorld.ancientCultistDiamond,
				NPCID.MoonLordCore      => !StorageWorld.moonlordDiamond,
				NPCID.QueenSlimeBoss    => !StorageWorld.queenSlimeDiamond,
				NPCID.HallowBoss        => !StorageWorld.empressDiamond,
				_                       => false //Default to false to shove everything else under the rug
			};

		public bool CanShowItemDropInUI() => true; //Don't make the item show up in the bestiary

		public string GetConditionDescription() => "Dropped on first kill of every vanilla boss";
	}
}
