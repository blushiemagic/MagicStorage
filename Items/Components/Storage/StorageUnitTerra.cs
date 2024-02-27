using MagicStorage.Components;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnitTerra : BaseStorageUnitItem<StorageUnitLuminite, UpgradeTerra>
	{
		public override StorageUnitTier Tier => StorageUnitTier.Terra;

		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Purple;
			Item.value = Item.sellPrice(silver: 12);
		}
	}
}
