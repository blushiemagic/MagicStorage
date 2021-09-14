using System;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedFurnitureStations2Item : CombinedStationsItem<CombinedFurnitureStations2Tile>
	{
		public override string ItemName => "Combined Furniture Stations (Tier 2)";

		public override string ItemDescription => "Combines the functionality of several crafting stations for furniture";

		public override int Rarity => ItemRarityID.Pink;

		public override void GetItemDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<CombinedFurnitureStations1Item>())
				.AddIngredient(ItemID.LesionStation)
				.AddIngredient(ItemID.FleshCloningVaat)
				.AddIngredient(ItemID.SteampunkBoiler)
				.AddIngredient(ItemID.LihzahrdFurnace)
				.Register();
		}
	}
}
