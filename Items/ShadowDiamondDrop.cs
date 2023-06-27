using MagicStorage.Common.Systems;
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
			IItemDropRule rule = null;

			switch (npc.type)
			{
				case NPCID.KingSlime:
					rule = DropDiamond(1);
					break;
				case NPCID.EyeofCthulhu:
					rule = DropDiamond(1, 2);
					break;
				case NPCID.EaterofWorldsHead:
				case NPCID.EaterofWorldsBody:
				case NPCID.EaterofWorldsTail:
				case NPCID.BrainofCthulhu:
				case NPCID.SkeletronHead:
				case NPCID.QueenBee:
				case NPCID.Deerclops:
					rule = DropDiamond(1);
					break;
				case NPCID.WallofFlesh:
					rule = DropDiamond(2);
					break;
				// Hardmode
				case NPCID.TheDestroyer:
				case NPCID.Retinazer:
				case NPCID.Spazmatism:
				case NPCID.SkeletronPrime:
					rule = DropDiamond(1);
					break;
				case NPCID.Plantera:
					rule = DropDiamond(1, 2);
					break;
				case NPCID.Golem:
				case NPCID.DukeFishron:
				case NPCID.CultistBoss:
					rule = DropDiamond(1);
					break;
				case NPCID.MoonLordCore:
					rule = DropDiamond(2, 3);
					break;
				case NPCID.QueenSlimeBoss:
				case NPCID.HallowBoss: // Empress of Light
					rule = DropDiamond(1);
					break;
			}

			if (CanModdedNPCDrop(npc) && StorageWorld.moddedDiamondDropRulesByType.TryGetValue(npc.type, out IItemDropRule dropRule))
				rule = dropRule;

			if (rule is not null)
				npcLoot.Add(rule);
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
					if (npc.boss)
						NPC.SetEventFlagCleared(ref StorageWorld.boss2Diamond, -1);
					break;
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

			if (CanModdedNPCDrop(npc)) {
				bool exists = StorageWorld.moddedDiamonds.Contains(npc.type);

				NPC.SetEventFlagCleared(ref exists, -1);

				StorageWorld.moddedDiamonds.Add(npc.type);
			}
		}

		private static IItemDropRule Drop(int count) => ItemDropRule.Common(ModContent.ItemType<ShadowDiamond>(), minimumDropped: count, maximumDropped: count);

		public static IItemDropRule DropDiamond(int stack, int expertStack = -1)
		{
			IItemDropRule rule = new LeadingConditionRule(new ShadowDiamondCondition());
			rule.OnSuccess(expertStack < 0 ? Drop(stack) : new DropBasedOnExpertMode(Drop(stack), Drop(expertStack)));

			return rule;
		}

		public static bool CanModdedNPCDrop(NPC npc) => npc.ModNPC is not null && npc.boss && !StorageWorld.disallowDropModded.Contains(npc.type);
	}

	internal class ShadowDiamondCondition : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) =>
			!info.IsInSimulation &&
			info.npc.type switch
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
			}
			|| (ShadowDiamondDrop.CanModdedNPCDrop(info.npc) && !StorageWorld.moddedDiamonds.Contains(info.npc.type));

		public bool CanShowItemDropInUI() => true; //Don't make the item show up in the bestiary

		public string GetConditionDescription() => "Dropped on first kill of every boss";
	}
}
