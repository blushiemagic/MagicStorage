using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class StorageUnitTerra : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Terra Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Блок хранения Терры");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Terra)");
		}

		public override void SetDefaults()
		{
			item.width = 26;
			item.height = 26;
			item.maxStack = 99;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.useStyle = 1;
			item.consumable = true;
			item.rare = 11;
			item.value = Item.sellPrice(0, 0, 12, 0);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 7;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnitLuminite"));
			recipe.AddIngredient(mod.ItemType("UpgradeTerra"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
