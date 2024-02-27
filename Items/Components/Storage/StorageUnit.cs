using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class StorageUnit : BaseStorageUnitItem
	{
		public override Components.StorageUnitTier Tier => Components.StorageUnitTier.Basic;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Item.ResearchUnlockCount = 30;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(silver: 6);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<StorageComponent>();
			recipe.AddRecipeGroup("MagicStorage:AnyChest");
			recipe.AddRecipeGroup("MagicStorage:AnySilverBar", 10);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
