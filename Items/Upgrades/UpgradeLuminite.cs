using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class UpgradeLuminite : BaseStorageUpgradeItem
	{
		public override void SetDefaults()
		{
			Item.rare = ItemRarityID.Red;
			Item.value = Item.sellPrice(gold: 1, silver: 50);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.LunarBar, 10);
			recipe.AddIngredient(ItemID.FragmentSolar, 5);
			recipe.AddIngredient(ItemID.FragmentVortex, 5);
			recipe.AddIngredient(ItemID.FragmentNebula, 5);
			recipe.AddIngredient(ItemID.FragmentStardust, 5);
			recipe.AddIngredient(ItemID.Ruby);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.Register();
		}
	}
}
