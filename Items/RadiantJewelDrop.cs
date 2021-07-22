using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
				var rule = new LeadingConditionRule(new Conditions.NotExpert());

				//1 out of 20 chance to drop 1 item
				rule.OnSuccess(new DropOneByOne(ModContent.ItemType<RadiantJewel>(), new DropOneByOne.Parameters(){
					ChanceNumerator = 1,
					ChanceDenominator = 20,
					MinimumStackPerChunkBase = 1,
					MaximumStackPerChunkBase = 1,
					MinimumItemDropsCount = 1,
					MaximumItemDropsCount = 1
				}));

				npcLoot.Add(rule);
			}
		}
	}
}
