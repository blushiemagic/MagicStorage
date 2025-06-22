using MagicStorage.CrossMod.Storage;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class UpgradeDemonite : BaseStorageUpgradeItem
	{
		public override StorageUnitTier Tier => StorageUnitTier.Demonite;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.value = Item.sellPrice(silver: 32);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.DemoniteBar, 10);
			recipe.AddIngredient(ItemID.Amethyst);
			recipe.AddTile(TileID.Anvils);
			recipe.Register();
		}
	}
}
