using MagicStorage.Components;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnitHellstone : BaseStorageUnitItem
	{
		public override StorageUnitTier Tier => StorageUnitTier.Hellstone;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 10;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Green;
			Item.value = Item.sellPrice(silver: 50);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<StorageUnitDemonite>();
			recipe.AddIngredient<UpgradeHellstone>();
			recipe.Register();

			recipe = CreateRecipe();
			recipe.AddIngredient<StorageUnitCrimtane>();
			recipe.AddIngredient<UpgradeHellstone>();
			recipe.Register();
		}
	}
}
