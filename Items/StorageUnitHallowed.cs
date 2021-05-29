using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorageExtra.Items
{
	public class StorageUnitHallowed : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Hallowed Storage Unit");
			DisplayName.AddTranslation(GameCulture.Russian, "Святая Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Jednostka magazynująca (Święcona)");
			DisplayName.AddTranslation(GameCulture.French, "Unité de stockage (Sacré)");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento (Sagrado)");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储单元(神圣)");
		}

		public override void SetDefaults()
		{
			item.width = 26;
			item.height = 26;
			item.maxStack = 99;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.consumable = true;
			item.rare = ItemRarityID.LightRed;
			item.value = Item.sellPrice(0, 1);
			item.createTile = ModContent.TileType<Components.StorageUnit>();
			item.placeStyle = 4;
		}

		public override void AddRecipes()
		{
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ModContent.ItemType<StorageUnitHellstone>());
			recipe.AddIngredient(ModContent.ItemType<UpgradeHallowed>());
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}