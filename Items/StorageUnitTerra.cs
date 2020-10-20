using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitTerra : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Terra Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Терра Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Terra)");
			DisplayName.AddTranslation(GameCulture.French, "Unité de stockage (Terra)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento (Tierra)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储单元(泰拉)");
		}

		public override void SetDefaults() {
			item.width = 26;
			item.height = 26;
			item.maxStack = 99;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.consumable = true;
			item.rare = ItemRarityID.Purple;
			item.value = Item.sellPrice(0, 0, 12);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 7;
		}

		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnitLuminite"));
			recipe.AddIngredient(mod.ItemType("UpgradeTerra"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
