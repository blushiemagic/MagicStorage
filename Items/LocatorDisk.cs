using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class LocatorDisk : Locator
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Locator Drive");
			DisplayName.AddTranslation(GameCulture.Russian, "Локатор с CD Приводом");
			DisplayName.AddTranslation(GameCulture.Polish, "Dysk lokalizatora");
			DisplayName.AddTranslation(GameCulture.French, "Disque Localisateur");
			DisplayName.AddTranslation(GameCulture.Spanish, "Disco Locador");

			Tooltip.SetDefault("<right> Storage Heart to store location"
				+ "\n<right> Remote Storage Access to set it"
				+ "\nDoes not get destroyed upon use");
			Tooltip.AddTranslation(GameCulture.Russian, "<right> по Cердцу Хранилища чтобы запомнить его местоположение"
				+ "\n<right> на Модуль Удаленного Доступа к Хранилищу чтобы привязать его к Сердцу Хранилища"
				+ "\nНе пропадает при использовании");
			Tooltip.AddTranslation(GameCulture.Polish, "<right> na serce jednostki magazynującej, aby zapisać jej lokalizację"
				+ "\n<right> na bezprzewodowe okno dostępu aby je ustawić"
				+ "\nNie niszczy się po użyciu");
			Tooltip.AddTranslation(GameCulture.French, "<right> Cœur du Stockage pour enregistrer son emplacement"
				+ "\n<right> Stockage Éloigné pour le mettre en place"
				+ "\nN'est pas détruit lors de son utilisation");
			Tooltip.AddTranslation(GameCulture.Spanish, "<right> el Corazón de Almacenamiento para registrar su ubicación"
				+ "\n<right> el Acceso de Almacenamiento Remoto para establecerlo"
				+ "\nNo se destruye cuando se usa");
		}

		public override void SetDefaults()
		{
			item.width = 28;
			item.height = 28;
			item.maxStack = 1;
			item.rare = 10;
			item.value = Item.sellPrice(0, 5, 0, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.MartianConduitPlating, 25);
			recipe.AddIngredient(ItemID.LunarBar, 2);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}

