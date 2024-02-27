using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class UpgradeHellstone : BaseStorageUpgradeItem
	{
		public override void SetDefaults()
		{
			Item.rare = ItemRarityID.Green;
			Item.value = Item.sellPrice(silver: 40);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.HellstoneBar, 10);
			recipe.AddIngredient(ItemID.Topaz);
			recipe.AddTile(TileID.Anvils);
			recipe.Register();
		}
	}
}
