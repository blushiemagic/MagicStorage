using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class SnowBiomeEmulator : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Broken Snowglobe");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Сломанная Снежная Сфера");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Emulator Śnieżnego Biomu");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Emulateur de biome de neige");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Emulador de bioma de la nieve");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "雪地环境模拟器");

			Tooltip.SetDefault("Allows the Storage Crafting Interface to craft snow biome recipes");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Позволяет Модулю Создания Предметов создавать предметы требующие нахождения игрока в снежном биоме");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Dodaje funkcje do Interfejsu Rzemieślniczego, pozwalającą na wytwarzanie przedmiotów dostępnych jedynie w Śnieżnym Biomie");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Permet à L'interface de Stockage Artisanat de créer des recettes de biome de neige");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Permite la Interfaz de Elaboración de almacenamiento a hacer de recetas de bioma de la nieve");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "允许制作存储单元拥有雪地环境");

			Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(8, 8));

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.rare = ItemRarityID.Blue;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddRecipeGroup("MagicStorage:AnySnowBiomeBlock", 300);
			recipe.AddTile(null, "CraftingAccess");
			recipe.Register();
		}
	}
}
