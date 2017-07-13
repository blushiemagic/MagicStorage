using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class RemoteAccess : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Remote Storage Access");
			DisplayName.AddTranslation(GameCulture.Russian, "Дистанционный доступ к хранилищу");
			DisplayName.AddTranslation(GameCulture.Polish, "bezprzewodowe okno dostępu");
			DisplayName.AddTranslation(GameCulture.French, "Fenêtre d'accès éloigné");
			DisplayName.AddTranslation(GameCulture.Spanish, "Acceso a Almacenamiento Remoto");

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
			item.useStyle = 1;
			item.consumable = true;
			item.rare = 1;
			item.value = Item.sellPrice(0, 1, 72, 50);
			item.createTile = mod.TileType("RemoteAccess");
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 5);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Ruby, 10);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyRuby", 10);
			}
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
