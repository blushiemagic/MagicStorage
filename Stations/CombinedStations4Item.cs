using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations{
	public class CombinedStations4Item : CombinedStationsItem<CombinedStations4Tile>{
		public override string ItemName => "Combined Stations (Final Tier)";

		public override string ItemDescription => "Combines the functionality of all crafting stations and liquids";

		public override int Rarity => ItemRarityID.Purple;

		public override void GetItemDimensions(out int width, out int height){
			throw new System.NotImplementedException();
		}

		public override void AddRecipes(){
			CreateRecipe(1)
				.AddIngredient(ModContent.ItemType<CombinedStations3Item>(), 1)
				.AddIngredient(ModContent.ItemType<CombinedFurnitureStations2Item>(), 1)
				.AddIngredient(ItemID.Autohammer, 1)
				.AddIngredient(ItemID.LunarCraftingStation, 1)
				.AddIngredient(ItemID.LavaBucket, 30)
				.AddIngredient(ItemID.HoneyBucket, 30)
				.Register();
		}
	}
}
