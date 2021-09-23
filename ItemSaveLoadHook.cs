using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public class ItemSaveLoadHook : GlobalItem
	{
		public override void SaveData(Item item, TagCompound tag)
		{
			if (CraftingGUI.IsTestItem(item))
				tag["TestItem"] = true;
		}

		public override void LoadData(Item item, TagCompound tag)
		{
			if (tag.ContainsKey("TestItem"))
				CraftingGUI.MarkAsTestItem(item);
		}
	}
}
