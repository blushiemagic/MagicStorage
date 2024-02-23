using MagicStorage.Components;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnitBlueChlorophyte : BaseStorageUnitItem<StorageUnitHallowed, UpgradeBlueChlorophyte>
	{
		public override StorageUnitTier Tier => StorageUnitTier.BlueChlorophyte;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 10;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Lime;
			Item.value = Item.sellPrice(gold: 1, silver: 60);
		}
	}
}
