using System;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class CombinedFurnitureStations1Item : CombinedStationsItem<CombinedFurnitureStations1Tile>
	{
		public override string ItemName => "Combined Furniture Stations (Tier 1)";

		public override string ItemDescription => "Combines the functionality of several crafting stations for furniture";

		public override int Rarity => ItemRarityID.Green;

		public override void GetItemDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BoneWelder)
				.AddIngredient(ItemID.GlassKiln)
				.AddIngredient(ItemID.HoneyDispenser)
				.AddIngredient(ItemID.IceMachine)
				.AddIngredient(ItemID.LivingLoom)
				.AddIngredient(ItemID.SkyMill)
				.AddIngredient(ItemID.Solidifier)
				.Register();
		}
	}
}
