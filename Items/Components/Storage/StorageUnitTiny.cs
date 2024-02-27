using MagicStorage.Components;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnitTiny : BaseStorageUnitItem
	{
		public override StorageUnitTier Tier => StorageUnitTier.Tiny;

		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(silver: 6);
		}
	}
}
