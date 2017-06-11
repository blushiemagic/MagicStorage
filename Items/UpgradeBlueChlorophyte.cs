using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class UpgradeBlueChlorophyte : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Blue Chlorophyte Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Синее хлорофитовое улучшение хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Niebieski Chlorofit)");

			Tooltip.SetDefault("Upgrades Storage Unit to 240 capacity"
				+ "\n<right> a Hallowed Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Улучшает блок хранения до 240 вместимости"
			        + "\n<right> по святому блоку хранения для использования");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 240 miejsc"
				+ "\n<right> na Jednostkę magazynującą (Święconą), aby użyć");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 7;
			item.value = Item.sellPrice(0, 1, 0, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.ShroomiteBar, 5);
			recipe.AddIngredient(ItemID.SpectreBar, 5);
			recipe.AddIngredient(ItemID.BeetleHusk, 2);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Emerald);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyEmerald");
			}
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
