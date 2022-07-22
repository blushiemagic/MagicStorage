using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class RadiantJewelDrop : GlobalNPC
	{
		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
		{
			if (npc.type == NPCID.MoonLordCore)
			{
				//10% chance to drop 1 item in Normal Mode
				npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(),
					ModContent.ItemType<RadiantJewel>(),
					chanceDenominator: 10,
					minimumDropped: 1,
					maximumDropped: 1,
					chanceNumerator: 1));
			}
		}
	}
}
