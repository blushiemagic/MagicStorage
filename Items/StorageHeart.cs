using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageHeart : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Сердце Хранилища");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Serce Jednostki Magazynującej");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Cœur de Stockage");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Corazón de Almacenamiento");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储核心");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
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
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(gold: 1, silver: 35);
			Item.createTile = ModContent.TileType<Components.StorageHeart>();
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 2);
			recipe.AddIngredient(ItemID.Emerald, 3);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
