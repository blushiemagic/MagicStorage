using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageAccess : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.AddTranslation(GameCulture.Russian, "Модуль Доступа к Хранилищу");
			DisplayName.AddTranslation(GameCulture.Polish, "Okno dostępu do magazynu");
			DisplayName.AddTranslation(GameCulture.French, "Access de Stockage");
			DisplayName.AddTranslation(GameCulture.Spanish, "Acceso de Almacenamiento");
		}

		public override void SetDefaults() {
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
			item.value = Item.sellPrice(0, 0, 67, 50);
			item.createTile = mod.TileType("StorageAccess");
		}

		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 3);
			if (MagicStorage.legendMod == null)
				recipe.AddIngredient(ItemID.Topaz, 3);
			else
				recipe.AddRecipeGroup("MagicStorage:AnyTopaz", 3);
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
