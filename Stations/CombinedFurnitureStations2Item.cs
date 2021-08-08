using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations{
	public class CombinedFurnitureStations2Item : CombinedStationsItem<CombinedFurnitureStations2Tile>{
		public override string ItemName => "Combined Furniture Stations (Tier 2)";

		public override string ItemDescription => "Combines the functionality of several crafting stations for furniture";

		public override int Rarity => ItemRarityID.Pink;

		public override void GetItemDimensions(out int width, out int height){
			throw new System.NotImplementedException();
		}

		public override void AddRecipes(){
			CreateRecipe(1)
				.AddIngredient(ModContent.ItemType<CombinedFurnitureStations1Item>(), 1)
				.AddIngredient(ItemID.LesionStation, 1)
				.AddIngredient(ItemID.FleshCloningVaat, 1)
				.AddIngredient(ItemID.SteampunkBoiler, 1)
				.AddIngredient(ItemID.LihzahrdFurnace, 1)
				.Register();
		}
	}
}
