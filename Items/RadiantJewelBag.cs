using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class RadiantJewelBag : GlobalItem
	{
#if TML_2022_6
		public override void OpenVanillaBag(string context, Player player, int arg)
		{
			//18% chance to drop 1 item in Expert Mode
			//25% chance to drop 1 item in Master Mode
			float chance = Main.masterMode ? 0.25f : 0.18f;
			if (context == "bossBag" && arg == ItemID.MoonLordBossBag && Main.rand.NextFloat() < chance)
			{
				var source = player.GetSource_OpenItem(ItemID.MoonLordBossBag);
				player.QuickSpawnItem(source, ModContent.ItemType<RadiantJewel>());
			}
		}
#else
		public override void ModifyItemLoot(Item item, ItemLoot itemLoot) {
			//18% chance to drop 1 item in Expert Mode
			//25% chance to drop 1 item in Master Mode
			itemLoot.Add(ItemDropRule.ByCondition(new IsExpertNotMaster(),
				ModContent.ItemType<RadiantJewel>(),
				chanceDenominator: 50,
				minimumDropped: 1,
				maximumDropped: 1,
				chanceNumerator: 9));
			itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(),
				ModContent.ItemType<RadiantJewel>(),
				chanceDenominator: 4,
				minimumDropped: 1,
				maximumDropped: 1,
				chanceNumerator: 1));
		}
#endif
	}

#if !TML_2022_6
	public class IsExpertNotMaster : IItemDropRuleCondition {
		public bool CanDrop(DropAttemptInfo info) => Main.expertMode && !Main.masterMode;

		public bool CanShowItemDropInUI() => Main.expertMode && !Main.masterMode;

		public string GetConditionDescription() => new Conditions.IsExpert().GetConditionDescription();
	}
#endif
}
