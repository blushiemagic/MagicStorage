using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitCrimtane : ModItem
	{
		public override void SetDefaults()
		{
			item.name = "Crimtane Storage Unit";
			item.width = 26;
			item.height = 26;
			item.maxStack = 99;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.useStyle = 1;
			item.consumable = true;
			item.rare = 1;
			item.value = Item.sellPrice(0, 0, 32, 0);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 2;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnit"));
			recipe.AddIngredient(mod.ItemType("UpgradeCrimtane"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}