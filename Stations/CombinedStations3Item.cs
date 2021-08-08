using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations{
	public class CombinedStations3Item : CombinedStationsItem<CombinedStations3Tile>{
		public override string ItemName => "Combined Stations (Tier 3)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override int Rarity => ItemRarityID.Yellow;

		public override void GetItemDimensions(out int width, out int height){
			throw new System.NotImplementedException();
		}

		public override void AddRecipes(){
			CreateRecipe(1)
				.AddIngredient(ModContent.ItemType<CombinedStations2Item>(), 1)
				.AddIngredient(ItemID.ImbuingStation, 1)
				.AddRecipeGroup("MagicStorage:AnyHmAnvil", 1)
				.AddRecipeGroup("MagicStorage:AnyHmFurnace", 1)
				.AddRecipeGroup("MagicStorage:AnyBookcase", 1)
				.AddIngredient(ItemID.CrystalBall, 1)
				.AddIngredient(ItemID.BlendOMatic, 1)
				.AddIngredient(ItemID.MeatGrinder, 1)
				.Register();
		}
	}
}
