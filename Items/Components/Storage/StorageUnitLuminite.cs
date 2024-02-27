using MagicStorage.Components;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnitLuminite : BaseStorageUnitItem<StorageUnitBlueChlorophyte, UpgradeLuminite>
	{
		public override StorageUnitTier Tier => StorageUnitTier.Luminite;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 5;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Red;
			Item.value = Item.sellPrice(gold: 2, silver: 50);
		}
	}
}
