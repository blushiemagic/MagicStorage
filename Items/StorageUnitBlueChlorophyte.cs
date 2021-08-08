using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitBlueChlorophyte : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Blue Chlorophyte Storage Unit");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Синяя Хлорофитовая Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Jednostka magazynująca (Niebieski Chlorofit)");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Unité de stockage (Chlorophylle Bleu)");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Unidad de Almacenamiento (Clorofita Azul)");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储单元(蓝色叶绿)");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 10;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 99;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.rare = ItemRarityID.Lime;
			Item.value = Item.sellPrice(gold: 1, silver: 60);
			Item.createTile = ModContent.TileType<Components.StorageUnit>();
			Item.placeStyle = 5;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<StorageUnitHallowed>());
			recipe.AddIngredient(ModContent.ItemType<UpgradeBlueChlorophyte>());
			recipe.Register();
		}
	}
}
