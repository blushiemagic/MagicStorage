using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class CrimsonAltar : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 5;
		}

		public override void SetDefaults()
		{
			Item.width = 48;
			Item.height = 34;
			Item.rare = ItemRarityID.Green;
			Item.createTile = ModContent.TileType<EvilAltarTile>();
			Item.placeStyle = 1;
			Item.maxStack = 99;
			Item.value = Item.sellPrice(silver: 15);
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.useTurn = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.CrimtaneBar, 10)
				.AddIngredient(ItemID.TissueSample, 15)
				.AddTile(TileID.DemonAltar)
				.Register();
		}
	}
}
