using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public class PortableCraftingAccess : PortableAccess {
		public override void SetStaticDefaults() {
			SacrificeTotal = 1;
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Purple;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 28;
			Item.useTime = 28;
			Item.value = Item.sellPrice(gold: 10);
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				if (location.X >= 0 && location.Y >= 0)
				{
					Tile tile = Main.tile[location.X, location.Y];
					if (!tile.HasTile || tile.TileType != ModContent.TileType<Components.CraftingAccess>() || tile.TileFrameX != 0 || tile.TileFrameY != 0)
						Main.NewText(Language.GetTextValue("Mods.MagicStorage.PortableCraftingMissing"));
					else {
						OpenStorage(player, true);
					}
				}
				else
				{
					Main.NewText(Language.GetTextValue("Mods.MagicStorage.PortableCraftingUnlocated"));
				}
			}

			return true;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<LocatorDisk>();
			recipe.AddIngredient(ItemID.LunarBar, 15);
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 3);
			recipe.AddIngredient(ItemID.Sapphire, 3);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.Register();
		}
	}
}
