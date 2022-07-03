using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public class EnvironmentAccess : ModItem {
		public override void SetStaticDefaults() {
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 99;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(gold: 1, silver: 35);
			Item.createTile = ModContent.TileType<Components.EnvironmentAccess>();
		}

		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<StorageComponent>();
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 2);
			recipe.AddIngredient(ItemID.DirtBlock, 50);
			recipe.AddIngredient(ItemID.StoneBlock, 50);
			recipe.AddIngredient(ItemID.MudBlock, 50);
			recipe.AddIngredient(ItemID.SnowBlock, 50);
			recipe.AddIngredient(ItemID.SandBlock, 50);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
