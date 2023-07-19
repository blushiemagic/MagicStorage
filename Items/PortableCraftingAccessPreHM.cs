using Terraria;
using Terraria.ID;

namespace MagicStorage.Items {
	public class PortableCraftingAccessPreHM : PortableCraftingAccess {
		public override void SetStaticDefaults() {
			SacrificeTotal = 1;
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Orange;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 28;
			Item.useTime = 28;
			Item.value = Item.sellPrice(gold: 1, silver: 10);
		}

		public override bool GetEffectiveRange(out float playerToPylonRange, out int pylonToStorageTileRange) {
			playerToPylonRange = 500 * 16;  //500 tiles
			pylonToStorageTileRange = 50;
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<Locator>()
				.AddRecipeGroup(RecipeGroupID.Wood, 20)
				.AddRecipeGroup(RecipeGroupID.IronBar, 15)
				.AddRecipeGroup("MagicStorage:AnyDemoniteBar", 10)
				.AddRecipeGroup("MagicStorage:AnyDiamond", 3)
				.AddIngredient(ItemID.Sapphire, 3)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
