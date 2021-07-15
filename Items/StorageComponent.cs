using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorageExtra.Items
{
	public class StorageComponent : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.Russian, "Компонент Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Komponent Magazynu");
			DisplayName.AddTranslation(GameCulture.French, "Composant de Stockage");
			DisplayName.AddTranslation(GameCulture.Spanish, "Componente de Almacenamiento");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储组件");
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
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.consumable = true;
			item.rare = ItemRarityID.White;
			item.value = Item.sellPrice(0, 0, 1);
			item.createTile = ModContent.TileType<Components.StorageComponent>();
		}

		public override void AddRecipes()
		{
			var recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.Wood, 10);
			recipe.AddIngredient(ItemID.IronBar, 2);
			recipe.anyWood = true;
			recipe.anyIronBar = true;
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
