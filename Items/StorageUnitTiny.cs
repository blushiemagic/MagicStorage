﻿using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitTiny : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Tiny Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Малая Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Mała jednostka magazynująca");
			DisplayName.AddTranslation(GameCulture.French, "Unité de Stockage Miniscule");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento Minúsculo");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储单元(小)");
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
			item.rare = ItemRarityID.White;
			item.value = Item.sellPrice(0, 0, 6);
			item.createTile = ModContent.TileType<Components.StorageUnit>();
			item.placeStyle = 8;
		}
	}
}
