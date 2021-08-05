using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeBlueChlorophyte : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Blue Chlorophyte Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Синее Хлорофитовое Улучшение Ячейки");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Niebieski Chlorofit)");
			DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Chlorophylle Bleu)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Actualización de Unidad de Almacenamiento (Clorofita Azul)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储升级珠(蓝色叶绿)");

			Tooltip.SetDefault("Upgrades Storage Unit to 240 capacity" + "\n<right> a Hallowed Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Увеличивает количество слотов в Ячейке Хранилища до 240" + "\n<right> на Святой Ячейке Хранилища для улучшения");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 240 miejsc" + "\n<right> na Jednostkę magazynującą (Święconą), aby użyć");
			Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 240" + "\n<right> l'unité de stockage (Sacré) pour utiliser");
			Tooltip.AddTranslation(GameCulture.Spanish, "Capacidad de unidad de almacenamiento mejorada a 240" + "\n<right> en la unidad de almacenamiento (Sagrado) para utilizar");
			Tooltip.AddTranslation(GameCulture.Chinese, "将存储单元升级至240容量" + "\n<right>一个存储单元(神圣)可镶嵌");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = ItemRarityID.Lime;
			item.value = Item.sellPrice(0, 1);
		}

		public override void AddRecipes()
		{
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.ShroomiteBar, 5);
			recipe.AddIngredient(ItemID.SpectreBar, 5);
			recipe.AddIngredient(ItemID.BeetleHusk, 2);
			if (MagicStorage.legendMod is null)
				recipe.AddIngredient(ItemID.Emerald);
			else
				recipe.AddRecipeGroup("MagicStorage:AnyEmerald");
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
