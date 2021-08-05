using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeLuminite : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Luminite Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Люминитовое Улучшение Ячейки Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Luminowany)");
			DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Luminite)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Actualización de Unidad de Almacenamiento (Luminita)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储升级珠(夜明)");

			Tooltip.SetDefault("Upgrades Storage Unit to 320 capacity" + "\n<right> a Blue Chlorophyte Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Увеличивает количество слотов в Ячейке Хранилища до 320" + "\n<right> на Синей Хлорофитовой Ячейке Хранилища для улучшения");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 320 miejsc" + "\n<right> na Jednostkę magazynującą (Niebieski Chlorofit), aby użyć");
			Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 320" + "\n<right> l'unité de stockage (Chlorophylle Bleu) pour utiliser");
			Tooltip.AddTranslation(GameCulture.Spanish, "Capacidad de unidad de almacenamiento mejorada a 320" + "\n<right> en la unidad de almacenamiento (Clorofita Azul) para utilizar");
			Tooltip.AddTranslation(GameCulture.Chinese, "将存储单元升级至320容量" + "\n<right>一个存储单元(夜明)可镶嵌");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = ItemRarityID.Red;
			item.value = Item.sellPrice(0, 1, 50);
		}

		public override void AddRecipes()
		{
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.LunarBar, 10);
			recipe.AddIngredient(ItemID.FragmentSolar, 5);
			recipe.AddIngredient(ItemID.FragmentVortex, 5);
			recipe.AddIngredient(ItemID.FragmentNebula, 5);
			recipe.AddIngredient(ItemID.FragmentStardust, 5);
			if (MagicStorage.legendMod is null)
				recipe.AddIngredient(ItemID.Ruby);
			else
				recipe.AddRecipeGroup("MagicStorage:AnyRuby");
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
