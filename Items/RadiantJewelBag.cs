using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class RadiantJewelBag : GlobalItem
	{
		public override void OpenVanillaBag(string context, Player player, int arg)
		{
			if (context == "bossBag" && arg == ItemID.MoonLordBossBag && Main.rand.NextBool(10))
			{
				var source = player.GetSource_OpenItem(ItemID.MoonLordBossBag);
				player.QuickSpawnItem(source, ModContent.ItemType<RadiantJewel>());
			}
		}
	}
}
