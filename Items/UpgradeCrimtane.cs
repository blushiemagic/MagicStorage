using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class UpgradeCrimtane : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Crimtane Storage Upgrade");
			Tooltip.SetDefault("Upgrades Storage Unit to 80 capacity"
				+ "\n<right> a Storage Unit to use");
			DisplayName.AddTranslation(GameCulture.Russian, "Кримтановое улучшение хранилища");
			Tooltip.AddTranslation(GameCulture.Russian, "Улучшает блок хранения до 80 вместимости"
			        + "\n<right> по  блоку хранения для использования");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 1;
			item.value = Item.sellPrice(0, 0, 32, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.CrimtaneBar, 10);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Amethyst);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyAmethyst");
			}
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
