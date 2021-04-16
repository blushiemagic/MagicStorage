using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorageExtra.Items
{
	public class UpgradeDemonite : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Demonite Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Демонитовое Улучшение Ячейки Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Demonit)");
			DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Démonite)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Actualización de Unidad de Almacenamiento (Endemoniado)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储升级珠(魔金)");

			Tooltip.SetDefault("Upgrades Storage Unit to 80 capacity" + "\n<right> a Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Увеличивает количество слотов в Ячейке Хранилища до 80" + "\n<right> на Ячейке Хранилища для улучшения");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 80 miejsc" + "\n<right> na Jednostkę magazynującą (Standardową), aby użyć");
			Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 80" + "\n<right> l'unité de stockage pour utiliser");
			Tooltip.AddTranslation(GameCulture.Spanish, "Capacidad de unidad de almacenamiento mejorada a 80" + "\n<right> en la unidad de almacenamiento para utilizar");
			Tooltip.AddTranslation(GameCulture.Chinese, "将存储单元升级至80容量" + "\n<right>一个存储单元(魔金)可镶嵌");
		}

		public override void SetDefaults() {
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = ItemRarityID.Blue;
			item.value = Item.sellPrice(0, 0, 32);
		}

		public override void AddRecipes() {
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.DemoniteBar, 10);
			if (MagicStorageExtra.legendMod == null)
				recipe.AddIngredient(ItemID.Amethyst);
			else
				recipe.AddRecipeGroup("MagicStorageExtra:AnyAmethyst");
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
