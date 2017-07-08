using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class CreativeStorageUnit : ModItem
	{
	
		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.Russian, "Креативный блок хранения");
			DisplayName.AddTranslation(GameCulture.Polish, "Kreatywna Jednostka Magazynująca");
            DisplayName.AddTranslation(GameCulture.French, "Unité de Stockage Créatif");

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
			item.createTile = mod.TileType("CreativeStorageUnit");
		}
	}
}
