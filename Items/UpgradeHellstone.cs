using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeHellstone : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Hellstone Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Адское Улучшение Ячейки Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Piekielny kamień)");
			DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Infernale)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Actualización de Unidad de Almacenamiento (Piedra Infernal)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储升级珠(狱岩)");

			Tooltip.SetDefault("Upgrades Storage Unit to 120 capacity" + "\n<right> a Demonite/Crimtane Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Увеличивает количество слотов в Ячейке Хранилища до 120" + "\n<right> на Демонитовой/Кримтановой Ячейке Хранилища для улучшения");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 120 miejsc" + "\n<right> na Jednostkę magazynującą (Karmazynit/Demonit), aby użyć");
			Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 120" + "\n<right> l'unité de stockage (Démonite/Carmitane) pour utiliser");
			Tooltip.AddTranslation(GameCulture.Spanish, "Capacidad de unidad de almacenamiento mejorada a 120" + "\n<right> en la unidad de almacenamiento (Endemoniado/Carmesí) para utilizar");
			Tooltip.AddTranslation(GameCulture.Chinese, "将存储单元升级至120容量" + "\n<right>一个存储单元(血腥/魔金)可镶嵌");
		}

		public override void SetDefaults() {
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = ItemRarityID.Green;
			item.value = Item.sellPrice(0, 0, 40);
		}

		public override void AddRecipes() {
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.HellstoneBar, 10);
			if (MagicStorage.legendMod == null)
				recipe.AddIngredient(ItemID.Topaz);
			else
				recipe.AddRecipeGroup("MagicStorage:AnyTopaz");
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
