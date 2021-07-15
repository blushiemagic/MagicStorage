using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorageExtra.Items
{
	public class ShadowDiamondDrop : GlobalNPC
	{
		public override void NPCLoot(NPC npc)
		{
			switch (npc.type)
			{
				case NPCID.KingSlime when !StorageWorld.kingSlimeDiamond:
					DropDiamond(npc, 1);
					StorageWorld.kingSlimeDiamond = true;
					break;
				case NPCID.EyeofCthulhu when !StorageWorld.boss1Diamond:
					DropDiamond(npc, Main.expertMode ? 2 : 1);
					StorageWorld.boss1Diamond = true;
					break;
				case NPCID.EaterofWorldsHead:
				case NPCID.EaterofWorldsBody:
				case NPCID.EaterofWorldsTail:
				case NPCID.BrainofCthulhu:
					if (!StorageWorld.boss2Diamond)
					{
						DropDiamond(npc, 1);
						StorageWorld.boss2Diamond = true;
					}

					break;
				case NPCID.SkeletronHead when !StorageWorld.boss3Diamond:
					DropDiamond(npc, 1);
					StorageWorld.boss3Diamond = true;
					break;
				case NPCID.QueenBee when !StorageWorld.queenBeeDiamond:
					DropDiamond(npc, 1);
					StorageWorld.queenBeeDiamond = true;
					break;
				case NPCID.WallofFlesh when !StorageWorld.hardmodeDiamond:
					DropDiamond(npc, 2);
					StorageWorld.hardmodeDiamond = true;
					break;
				case NPCID.TheDestroyer when !StorageWorld.mechBoss1Diamond:
					DropDiamond(npc, 1);
					StorageWorld.mechBoss1Diamond = true;
					break;
				case NPCID.Retinazer:
				case NPCID.Spazmatism:
					if (!StorageWorld.mechBoss2Diamond)
					{
						DropDiamond(npc, 1);
						StorageWorld.mechBoss2Diamond = true;
					}

					break;
				case NPCID.SkeletronPrime when !StorageWorld.mechBoss3Diamond:
					DropDiamond(npc, 1);
					StorageWorld.mechBoss3Diamond = true;
					break;
				case NPCID.Plantera when !StorageWorld.plantBossDiamond:
					DropDiamond(npc, Main.expertMode ? 2 : 1);
					StorageWorld.plantBossDiamond = true;
					break;
				case NPCID.Golem when !StorageWorld.golemBossDiamond:
					DropDiamond(npc, 1);
					StorageWorld.golemBossDiamond = true;
					break;
				case NPCID.DukeFishron when !StorageWorld.fishronDiamond:
					DropDiamond(npc, 1);
					StorageWorld.fishronDiamond = true;
					break;
				case NPCID.CultistBoss when !StorageWorld.ancientCultistDiamond:
					DropDiamond(npc, 1);
					StorageWorld.ancientCultistDiamond = true;
					break;
				case NPCID.MoonLordCore when !StorageWorld.moonlordDiamond:
					DropDiamond(npc, Main.expertMode ? 3 : 2);
					StorageWorld.moonlordDiamond = true;
					break;
			}
		}

		private void DropDiamond(NPC npc, int stack)
		{
			Item.NewItem(npc.position, npc.width, npc.height, ModContent.ItemType<ShadowDiamond>(), stack);
		}
	}
}
