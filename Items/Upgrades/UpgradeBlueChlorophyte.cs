using MagicStorage.CrossMod.Storage;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Items
{
	public class UpgradeBlueChlorophyte : BaseStorageUpgradeItem
	{
		public override StorageUnitTier Tier => StorageUnitTier.BlueChlorophyte;

		public override void SetDefaults()
		{
			Item.value = Item.sellPrice(gold: 1);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.ShroomiteBar, 5);
			recipe.AddIngredient(ItemID.SpectreBar, 5);
			recipe.AddIngredient(ItemID.BeetleHusk, 2);
			recipe.AddIngredient(ItemID.Emerald);
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.Register();
		}
	}
}
