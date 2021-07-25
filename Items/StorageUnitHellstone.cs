﻿using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitHellstone : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Hellstone Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Адская Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Piekielny kamień)");
			DisplayName.AddTranslation(GameCulture.French, "Unité de stockage (Infernale)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento (Piedra Infernal)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储单元(狱岩)");
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
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.consumable = true;
			item.rare = ItemRarityID.Green;
			item.value = Item.sellPrice(0, 0, 50);
			item.createTile = ModContent.TileType<Components.StorageUnit>();
			item.placeStyle = 3;
		}

		public override void AddRecipes()
		{
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ModContent.ItemType<StorageUnitDemonite>());
			recipe.AddIngredient(ModContent.ItemType<UpgradeHellstone>());
			recipe.SetResult(this);
			recipe.AddRecipe();

			recipe = new ModRecipe(mod);
			recipe.AddIngredient(ModContent.ItemType<StorageUnitCrimtane>());
			recipe.AddIngredient(ModContent.ItemType<UpgradeHellstone>());
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
