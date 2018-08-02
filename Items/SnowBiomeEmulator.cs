using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class SnowBiomeEmulator : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Broken Snowglobe");
			DisplayName.AddTranslation(GameCulture.Russian, "Сломанная Снежная Сфера");
			DisplayName.AddTranslation(GameCulture.Polish, "Emulator Śnieżnego Biomu");
			DisplayName.AddTranslation(GameCulture.French, "Emulateur de biome de neige");
			DisplayName.AddTranslation(GameCulture.Spanish, "Emulador de bioma de la nieve");

			Tooltip.SetDefault("Allows the Storage Crafting Interface to craft snow biome recipes");
			Tooltip.AddTranslation(GameCulture.Russian, "Позволяет Модулю Создания Предметов создавать предметы требующие нахождения игрока в снежном биоме");
			Tooltip.AddTranslation(GameCulture.Polish, "Dodaje funkcje do Interfejsu Rzemieślniczego, pozwalającą na wytwarzanie przedmiotów dostępnych jedynie w Śnieżnym Biomie");
			Tooltip.AddTranslation(GameCulture.French, "Permet à L'interface de Stockage Artisanat de créer des recettes de biome de neige");
			Tooltip.AddTranslation(GameCulture.Spanish, "Permite la Interfaz de Elaboración de almacenamiento a hacer de recetas de bioma de la nieve");
			Main.RegisterItemAnimation(item.type, new DrawAnimationVertical(8, 8));
		}

		public override void SetDefaults()
		{
			item.width = 30;
			item.height = 30;
			item.rare = 1;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddRecipeGroup("MagicStorage:AnySnowBiomeBlock", 300);
			recipe.AddTile(null, "CraftingAccess");
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
