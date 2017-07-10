using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class StorageUnitDemonite : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Demonite Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Демонитовый блок хранения");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Demonit)");
			DisplayName.AddTranslation(GameCulture.French, "Unité de stockage (Démonite)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento (Endemoniado)");
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
			item.rare = 1;
			item.value = Item.sellPrice(0, 0, 32, 0);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 1;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnit"));
			recipe.AddIngredient(mod.ItemType("UpgradeDemonite"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
