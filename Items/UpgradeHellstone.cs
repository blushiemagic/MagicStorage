using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class UpgradeHellstone : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Hellstone Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Адское улучшение хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Piekielny kamień)");
			DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Infernale)");

			Tooltip.SetDefault("Upgrades Storage Unit to 120 capacity"
				+ "\n<right> a Demonite/Crimtane Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Улучшает блок хранения до 120 вместимости"
				+ "\n<right> по демонитовому/кримтановому блоку хранения для использования");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 120 miejsc"
				+ "\n<right> na Jednostkę magazynującą (Karmazynit/Demonit), aby użyć");
			Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 120"
				+ "\n<right> l'unité de stockage (Démonite/Carmitane) pour utiliser");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 2;
			item.value = Item.sellPrice(0, 0, 40, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.HellstoneBar, 10);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Topaz);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyTopaz");
			}
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
