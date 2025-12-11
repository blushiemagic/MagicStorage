using MagicStorage.Common.Players;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public class StorageSecurity : ModItem {
		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
			itemGroup = ContentSamples.CreativeHelper.ItemGroup.Wands;
		}

		public override void SetDefaults() {
			Item.width = 36;
			Item.height = 36;
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
				if (TileEntity.ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageCenter center) {
					if (SecuritySystem.CanPlayerAccessImmediately(player, center.assignedNetwork)) {
						player.GetModPlayer<SecurityPlayer>().RequestingSecurityUI = true;
						Components.StorageAccess.OpenStorage(player, center.Position.X, center.Position.Y);
					} else
						SecuritySystem.PrintStorageInaccessible();
				}
			}

			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.GoldenKey, 3)
				.AddIngredient(ItemID.Wire, 25)
				.AddRecipeGroup(RecipeGroupID.IronBar, 5)
				.AddIngredient<StorageComponent>()
				.AddIngredient(ItemID.Amber, 6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
