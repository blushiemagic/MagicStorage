using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations{
	public class CombinedStations2Item : CombinedStationsItem<CombinedStations2Tile>{
		public override string ItemName => "Combined Stations (Tier 2)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override int Rarity => ItemRarityID.Pink;

		public override void GetItemDimensions(out int width, out int height){
			throw new System.NotImplementedException();
		}

		public override void AddRecipes(){
			CreateRecipe(1)
				.AddIngredient(ModContent.ItemType<CombinedStations1Item>(), 1)
				.AddIngredient(ItemID.AlchemyTable, 1)
				.AddRecipeGroup("MagicStorage:AnyCookingPot", 1)
				.AddIngredient(ItemID.TinkerersWorkshop, 1)
				.AddIngredient(ItemID.DyeVat, 1)
				.AddIngredient(ItemID.HeavyWorkBench, 1)
				.AddIngredient(ItemID.Keg, 1)
				.AddIngredient(ItemID.TeaKettle, 1)
				.Register();
		}
	}
}
