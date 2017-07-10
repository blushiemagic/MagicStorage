using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class SnowBiomeEmulator : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.French, "Emulateur de biome de neige");
			DisplayName.AddTranslation(GameCulture.Spanish, "Emulador de bioma de la nieve");

			Tooltip.SetDefault("Allows the Storage Crafting Interface to craft snow biome recipes");
			Tooltip.AddTranslation(GameCulture.French, "Permet à L'interface de Stockage Artisanat de créer des recettes de biome de neige");
			Tooltip.AddTranslation(GameCulture.Spanish, "Permite la Interfaz de Elaboración de almacenamiento a hacer de recetas de bioma de la nieve");
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
