using System;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedStations4Item : CombinedStationsItem<CombinedStations4Tile>
	{
		public override string ItemName => "Combined Stations (Final Tier)";

		public override string ItemDescription => "Combines the functionality of all crafting stations and liquids";

		public override int Rarity => ItemRarityID.Purple;

		public override void GetItemDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<CombinedStations3Item>())
				.AddIngredient(ModContent.ItemType<CombinedFurnitureStations2Item>())
				.AddIngredient(ItemID.Autohammer)
				.AddIngredient(ItemID.LunarCraftingStation)
				.AddIngredient(ItemID.LavaBucket, 30)
				.AddIngredient(ItemID.HoneyBucket, 30)
				.Register();
		}
	}
}
