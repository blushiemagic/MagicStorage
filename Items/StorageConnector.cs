using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageConnector : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 100;
		}

		public override void SetDefaults()
		{
			Item.width = 12;
			Item.height = 12;
			Item.maxStack = 999;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(copper: 10);
			Item.createTile = ModContent.TileType<Components.StorageConnector>();
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe(16);
			recipe.AddRecipeGroup(RecipeGroupID.Wood, 16);
			recipe.AddRecipeGroup(RecipeGroupID.IronBar);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
