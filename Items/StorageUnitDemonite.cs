using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorageExtra.Items
{
	public class StorageUnitDemonite : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Demonite Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Демонитовая Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Demonit)");
			DisplayName.AddTranslation(GameCulture.French, "Unité de stockage (Démonite)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento (Endemoniado)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储单元(魔金)");
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
			item.rare = ItemRarityID.Blue;
			item.value = Item.sellPrice(0, 0, 32);
			item.createTile = mod.TileType("StorageUnit");
			item.placeStyle = 1;
		}

		public override void AddRecipes() {
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(mod.ItemType("StorageUnit"));
			recipe.AddIngredient(mod.ItemType("UpgradeDemonite"));
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
