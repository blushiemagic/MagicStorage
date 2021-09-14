using System;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedStations3Item : CombinedStationsItem<CombinedStations3Tile>
	{
		public override string ItemName => "Combined Stations (Tier 3)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override int Rarity => ItemRarityID.Yellow;

		public override void GetItemDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<CombinedStations2Item>())
				.AddIngredient(ItemID.ImbuingStation)
				.AddRecipeGroup("MagicStorage:AnyHmAnvil")
				.AddRecipeGroup("MagicStorage:AnyHmFurnace")
				.AddRecipeGroup("MagicStorage:AnyBookcase")
				.AddIngredient(ItemID.CrystalBall)
				.AddIngredient(ItemID.BlendOMatic)
				.AddIngredient(ItemID.MeatGrinder)
				.Register();
		}
	}
}
