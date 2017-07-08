using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
namespace MagicStorage.Items
{
	public class UpgradeLuminite : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Luminite Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Люминитовое улучшение хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Luminowany)");
			DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Luminite)");

			Tooltip.SetDefault("Upgrades Storage Unit to 320 capacity"
				+ "\n<right> a Blue Chlorophyte Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Улучшает бБлок хранения до 320 вместимости"
				+ "\n<right> по синему хлорофитовому блоку хранения для использования");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 320 miejsc"
				+ "\n<right> na Jednostkę magazynującą (Niebieski Chlorofit), aby użyć");
			Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 320"
				+ "\n<right> l'unité de stockage (Chlorophylle Bleu) pour utiliser");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 10;
			item.value = Item.sellPrice(0, 1, 50, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.LunarBar, 10);
			recipe.AddIngredient(ItemID.FragmentSolar, 5);
			recipe.AddIngredient(ItemID.FragmentVortex, 5);
			recipe.AddIngredient(ItemID.FragmentNebula, 5);
			recipe.AddIngredient(ItemID.FragmentStardust, 5);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Ruby);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyRuby");
			}
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
