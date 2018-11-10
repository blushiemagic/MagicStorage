using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class StorageHeart : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.Russian, "Сердце Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Serce Jednostki Magazynującej");
			DisplayName.AddTranslation(GameCulture.French, "Cœur de Stockage");
			DisplayName.AddTranslation(GameCulture.Spanish, "Corazón de Almacenamiento");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储核心");
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
			item.value = Item.sellPrice(0, 1, 35, 0);
			item.createTile = mod.TileType("StorageHeart");
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 3);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Emerald, 7);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyEmerald", 7);
			}
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
