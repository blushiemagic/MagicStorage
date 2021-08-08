using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public class ItemSaveLoadHook : GlobalItem
	{
		public override TagCompound Save(Item item)
		{
			if (CraftingGUI.IsTestItem(item))
				return new TagCompound { { "TestItem", true } };

			return null;
		}

		public override void Load(Item item, TagCompound tag)
		{
			if (tag is not null && tag.ContainsKey("TestItem"))
				CraftingGUI.MarkAsTestItem(item);

			base.Load(item, tag);
		}

		public override bool NeedsSaving(Item item) => CraftingGUI.IsTestItem(item);
	}
}
