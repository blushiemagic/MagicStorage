using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeHallowed : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Hallowed Storage Upgrade");
			DisplayName.AddTranslation(GameCulture.Russian, "Святое Улучшение Ячейки Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Święcone)");
			DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Sacré)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Actualización de Unidad de Almacenamiento (Sagrado)");

			Tooltip.SetDefault("Upgrades Storage Unit to 160 capacity" + "\n<right> a Hellstone Storage Unit to use");
			Tooltip.AddTranslation(GameCulture.Russian, "Увеличивает количество слотов в Ячейке Хранилища до 160" + "\n<right> на Адской Ячейке Хранилища для улучшения");
			Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 160 miejsc" + "\n<right> na Jednostkę magazynującą (Piekielny kamień), aby użyć");
			Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 160" + "\n<right> l'unité de stockage (Infernale) pour utiliser");
			Tooltip.AddTranslation(GameCulture.Spanish, "Capacidad de unidad de almacenamiento mejorada a 160" + "\n<right> en la unidad de almacenamiento (Piedra Infernal) para utilizar");
			Tooltip.AddTranslation(GameCulture.Chinese, "将存储单元升级至160容量" + "\n<right>一个存储单元(神圣)可镶嵌");
		}

		public override void SetDefaults() {
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = ItemRarityID.LightRed;
			item.value = Item.sellPrice(0, 0, 40);
		}

		public override void AddRecipes() {
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.HallowedBar, 10);
			recipe.AddIngredient(ItemID.SoulofFright);
			recipe.AddIngredient(ItemID.SoulofMight);
			recipe.AddIngredient(ItemID.SoulofSight);
			if (MagicStorage.legendMod == null)
				recipe.AddIngredient(ItemID.Sapphire);
			else
				recipe.AddRecipeGroup("MagicStorage:AnySapphire");
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
