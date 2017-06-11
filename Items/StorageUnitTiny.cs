using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class StorageUnitTiny : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Tiny Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Маленький блок хранения");
			DisplayName.AddTranslation(GameCulture.Polish, "Mała jednostka magazynująca");
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
			item.rare = 0;
			item.value = Item.sellPrice(0, 0, 6, 0);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 8;
		}
	}
}
