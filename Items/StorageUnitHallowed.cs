using MagicStorage.Components;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnitHallowed : BaseStorageUnitItem<StorageUnitHellstone, UpgradeHallowed>
	{
		public override StorageUnitTier Tier => StorageUnitTier.Hallowed;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 10;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.sellPrice(gold: 1);
		}
	}
}
