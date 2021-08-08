using System;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class CombinedStations2Item : CombinedStationsItem<CombinedStations2Tile>
	{
		public override string ItemName => "Combined Stations (Tier 2)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override int Rarity => ItemRarityID.Pink;

		public override void GetItemDimensions(out int width, out int height)
		{
			throw new NotImplementedException();
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<CombinedStations1Item>())
				.AddIngredient(ItemID.AlchemyTable)
				.AddRecipeGroup("MagicStorage:AnyCookingPot")
				.AddIngredient(ItemID.TinkerersWorkshop)
				.AddIngredient(ItemID.DyeVat)
				.AddIngredient(ItemID.HeavyWorkBench)
				.AddIngredient(ItemID.Keg)
				.AddIngredient(ItemID.TeaKettle)
				.Register();
		}
	}
}
