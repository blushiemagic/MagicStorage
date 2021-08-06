using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageComponent : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Компонент Хранилища");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Komponent Magazynu");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Composant de Stockage");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Componente de Almacenamiento");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储组件");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 30;
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
			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(0, 0, 1);
			Item.createTile = ModContent.TileType<Components.StorageComponent>();
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddRecipeGroup(RecipeGroupID.Wood, 10);
			recipe.AddRecipeGroup(RecipeGroupID.IronBar, 2);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
