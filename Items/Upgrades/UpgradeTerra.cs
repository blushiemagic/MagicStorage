using MagicStorage.CrossMod.Storage;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class UpgradeTerra : BaseStorageUpgradeItem
	{
		public override StorageUnitTier Tier => StorageUnitTier.Terra;

		public override void SetDefaults()
		{
			Item.value = Item.sellPrice(gold: 10);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<RadiantJewel>();
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.Register();
		}
	}
}
