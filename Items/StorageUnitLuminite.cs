using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class StorageUnitLuminite : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Luminite Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Люминитовый блок хранения");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Luminowana)");
			DisplayName.AddTranslation(GameCulture.French, "Unité de stockage (Luminite)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento (Luminita)");
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
			item.rare = 10;
			item.value = Item.sellPrice(0, 2, 50, 0);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 6;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnitBlueChlorophyte"));
			recipe.AddIngredient(mod.ItemType("UpgradeLuminite"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
