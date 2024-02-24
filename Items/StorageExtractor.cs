using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public class StorageExtractor : ModItem {
		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 28;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 15;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.tileBoost = 20;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(silver: 40);
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer && player.itemAnimation > 0 && player.itemTime == 0 && player.controlUseItem) {
				int i = Player.tileTargetX;
				int j = Player.tileTargetY;
				if (Main.tile[i, j].TileFrameX % 36 == 18)
					i--;
				if (Main.tile[i, j].TileFrameY % 36 == 18)
					j--;

				Point16 point = new(i, j);
				if (TileEntity.ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageUnit storage)
					storage.RemoveItemsAndSpawnCore();
			}

			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.Wrench)
				.AddIngredient<StorageComponent>()
				.AddIngredient(ItemID.Wire, 10)
				.AddRecipeGroup(RecipeGroupID.IronBar, 5)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
