using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class UpgradeTerra : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Terra Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Улучшение Терры");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Terra)");

			Tooltip.SetDefault("Upgrades Storage Unit to 640 capacity"
				+ "\n<right> a Luminite Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Улучшает блок хранения до 640 вместимости"
			        + "\n<right> по люминитовому блоку хранения для использования");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 640 miejsc"
				+ "\n<right> na Jednostkę magazynującą (Luminowaną), aby użyć");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 11;
			item.value = Item.sellPrice(0, 10, 0, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(null, "RadiantJewel");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();

			Mod otherMod = MagicStorage.bluemagicMod;
			if (otherMod != null)
			{
				recipe = new ModRecipe(mod);
				recipe.AddIngredient(otherMod, "InfinityCrystal");
				recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
				recipe.AddTile(otherMod, "PuriumAnvil");
				recipe.SetResult(this);
				recipe.AddRecipe();
			}

			otherMod = ModLoader.GetMod("CalamityMod");
			if (otherMod != null)
			{
				recipe = new ModRecipe(mod);
				recipe.AddIngredient(otherMod, "CosmiliteBar", 20);
				recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
				recipe.AddTile(TileID.LunarCraftingStation);
				recipe.SetResult(this);
				recipe.AddRecipe();
			}
		}
	}
}
