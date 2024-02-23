using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class UpgradeHallowed : BaseStorageUpgradeItem
	{
		public override void SetDefaults()
		{
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.sellPrice(silver: 40);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.HallowedBar, 10);
			recipe.AddIngredient(ItemID.SoulofFright);
			recipe.AddIngredient(ItemID.SoulofMight);
			recipe.AddIngredient(ItemID.SoulofSight);
			recipe.AddIngredient(ItemID.Sapphire);
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.Register();
		}
	}
}
