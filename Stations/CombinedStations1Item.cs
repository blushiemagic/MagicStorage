using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations{
	public class CombinedStations1Item : CombinedStationsItem<CombinedStations1Tile>{
		public override string ItemName => "Combined Stations (Tier 1)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override int Rarity => ItemRarityID.Green;

		public override void GetItemDimensions(out int width, out int height){
			throw new System.NotImplementedException();
		}

		public override void AddRecipes(){
			CreateRecipe(1)
				.AddRecipeGroup("MagicStorage:AnyWorkBench", 1)
				.AddIngredient(ItemID.Furnace, 1)
				.AddRecipeGroup("MagicStorage:AnyPreHmAnvil", 1)
				.AddRecipeGroup("MagicStorage:AnyBottle", 1)
				.AddRecipeGroup("MagicStorage:AnySink", 1)
				.AddIngredient(ItemID.Sawmill, 1)
				.AddIngredient(ItemID.Loom, 1)
				.AddRecipeGroup("MagicStorage:AnyTable", 1)
				.Register();
		}
	}
}
