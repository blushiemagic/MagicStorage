using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class StorageUnitHellstone : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Hellstone Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Адский блок хранения");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Piekielny kamień)");
		    	DisplayName.AddTranslation(GameCulture.French, "Unité de stockage (Infernale)");
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
			item.rare = 2;
			item.value = Item.sellPrice(0, 0, 50, 0);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 3;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnitDemonite"));
			recipe.AddIngredient(mod.ItemType("UpgradeHellstone"));
			recipe.SetResult(this);
			recipe.AddRecipe();

			recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnitCrimtane"));
			recipe.AddIngredient(mod.ItemType("UpgradeHellstone"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
