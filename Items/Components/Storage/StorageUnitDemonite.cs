using MagicStorage.Components;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnitDemonite : BaseStorageUnitItem<StorageUnit, UpgradeDemonite>
	{
		public override StorageUnitTier Tier => StorageUnitTier.Demonite;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 10;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(silver: 32);
		}
	}
}
