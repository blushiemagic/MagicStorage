using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class LocatorDisk : Locator
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Locator Drive");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Локатор с CD Приводом");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Dysk lokalizatora");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Disque Localisateur");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Disco Locador");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "定位器驱动");

			Tooltip.SetDefault("<right> Storage Heart to store location" + "\n<right> Remote Storage Access to set it" + "\nDoes not get destroyed upon use");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "<right> по Cердцу Хранилища чтобы запомнить его местоположение" + "\n<right> на Модуль Удаленного Доступа к Хранилищу чтобы привязать его к Сердцу Хранилища" + "\nНе пропадает при использовании");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "<right> na serce jednostki magazynującej, aby zapisać jej lokalizację" + "\n<right> na bezprzewodowe okno dostępu aby je ustawić" + "\nNie niszczy się po użyciu");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "<right> Cœur du Stockage pour enregistrer son emplacement" + "\n<right> Stockage Éloigné pour le mettre en place" + "\nN'est pas détruit lors de son utilisation");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "<right> el Corazón de Almacenamiento para registrar su ubicación" + "\n<right> el Acceso de Almacenamiento Remoto para establecerlo" + "\nNo se destruye cuando se usa");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "<right>存储核心可储存其定位点" + "\n<right>远程存储装置以设置其定位点" + "\n使用后不再损坏");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.sellPrice(0, 5);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.MartianConduitPlating, 25);
			recipe.AddIngredient(ItemID.LunarBar, 2);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.Register();
		}
	}
}
