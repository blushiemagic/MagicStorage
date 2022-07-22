using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class RadiantJewelBag : GlobalItem
	{
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
	}
}
