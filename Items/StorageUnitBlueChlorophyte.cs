using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitBlueChlorophyte : ModItem
	{
		public override void SetDefaults()
		{
			item.name = "Blue Chlorophyte Storage Unit";
			item.width = 26;
			item.height = 26;
			item.maxStack = 99;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.useStyle = 1;
			item.consumable = true;
			item.rare = 7;
			item.value = Item.sellPrice(0, 1, 60, 0);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 5;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnitHallowed"));
			recipe.AddIngredient(mod.ItemType("UpgradeBlueChlorophyte"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}