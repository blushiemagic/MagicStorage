using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;

namespace MagicStorage.Items {
	public class PortableCraftingAccessHM : PortableCraftingAccess {
		public override void SetStaticDefaults() {
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Lime;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 28;
			Item.useTime = 28;
			Item.value = Item.sellPrice(gold: 3);
		}

		public override bool GetEffectiveRange(out float playerToPylonRange, out int pylonToStorageTileRange) {
			playerToPylonRange = 1500 * 16;  //1500 tiles
			pylonToStorageTileRange = 100;
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<PortableCraftingAccessPreHM>()
				.AddIngredient(ItemID.Pearlwood, 20)
				.AddRecipeGroup("MagicStorage:AnyMythrilBar", 15)
				.AddIngredient(ItemID.ChlorophyteBar, 10)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
