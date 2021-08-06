using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageAccess : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Модуль Доступа к Хранилищу");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Okno dostępu do magazynu");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Access de Stockage");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Acceso de Almacenamiento");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储装置");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 5;
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
			Item.value = Item.sellPrice(0, 0, 67, 50);
			Item.createTile = ModContent.TileType<Components.StorageAccess>();
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
			recipe.AddIngredient(ItemID.Topaz, 3);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
