using System;
using Terraria.ID;

namespace MagicStorage.Stations
{
	public class CombinedStations1Item : CombinedStationsItem<CombinedStations1Tile>
	{
		public override string ItemName => "Combined Stations (Tier 1)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override int Rarity => ItemRarityID.Green;

		public override void GetItemDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddRecipeGroup("MagicStorage:AnyWorkBench")
				.AddIngredient(ItemID.Furnace)
				.AddRecipeGroup("MagicStorage:AnyPreHmAnvil")
				.AddRecipeGroup("MagicStorage:AnyBottle")
				.AddRecipeGroup("MagicStorage:AnySink")
				.AddIngredient(ItemID.Sawmill)
				.AddIngredient(ItemID.Loom)
				.AddRecipeGroup("MagicStorage:AnyTable")
				.Register();
		}
	}
}
