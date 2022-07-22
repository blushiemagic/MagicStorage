using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class RadiantJewelBag : GlobalItem
	{
		public override void OpenVanillaBag(string context, Player player, int arg)
		{
			//18% chance to drop 1 item
			if (context == "bossBag" && arg == ItemID.MoonLordBossBag && Main.rand.NextFloat() < 0.18f)
			{
				var source = player.GetSource_OpenItem(ItemID.MoonLordBossBag);
				player.QuickSpawnItem(source, ModContent.ItemType<RadiantJewel>());
			}
		}
	}
}
