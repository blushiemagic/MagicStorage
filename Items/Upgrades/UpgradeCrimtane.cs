using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class UpgradeCrimtane : BaseStorageUpgradeItem
	{
		public override void SetDefaults()
		{
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(silver: 32);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.CrimtaneBar, 10);
			recipe.AddIngredient(ItemID.Amethyst);
			recipe.AddTile(TileID.Anvils);
			recipe.Register();
		}
	}
}
