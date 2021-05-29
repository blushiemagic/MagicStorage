using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorageExtra.Items
{
	public class RadiantJewelDrop : GlobalNPC
	{
		public override void NPCLoot(NPC npc)
		{
			if (npc.type == NPCID.MoonLordCore && !Main.expertMode && Main.rand.Next(20) == 0)
				Item.NewItem((int) npc.position.X, (int) npc.position.Y, npc.width, npc.height, ModContent.ItemType<RadiantJewel>());
		}
	}
}