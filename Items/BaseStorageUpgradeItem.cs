using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public abstract class BaseStorageUpgradeItem : ModItem {
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 10;
		}

		public override void SetDefaults() {
			Item.width = 12;
			Item.height = 12;
			Item.maxStack = 99;
		}
	}
}
