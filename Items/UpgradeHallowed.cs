using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class UpgradeHallowed : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Hallowed Storage Upgrade");
			Tooltip.SetDefault("Upgrades Storage Unit to 160 capacity"
				+ "\n<right> a Hellstone Storage Unit to use");
			DisplayName.AddTranslation(GameCulture.Russian, "Святое улучшение хранилища");
			Tooltip.AddTranslation(GameCulture.Russian, "Улучшает Блок Хранения до 160 вместимости"
			        + "\n<right> по Адскому Блоку Хранения для использования);	
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 4;
			item.value = Item.sellPrice(0, 0, 40, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.HallowedBar, 10);
			recipe.AddIngredient(ItemID.SoulofFright);
			recipe.AddIngredient(ItemID.SoulofMight);
			recipe.AddIngredient(ItemID.SoulofSight);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Sapphire);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnySapphire");
			}
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
