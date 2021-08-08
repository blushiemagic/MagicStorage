using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations{
	public class CombinedFurnitureStations1Item : CombinedStationsItem<CombinedFurnitureStations1Tile>{
		public override string ItemName => "Combined Furniture Stations (Tier 1)";

		public override string ItemDescription => "Combines the functionality of several crafting stations for furniture";

		public override int Rarity => ItemRarityID.Green;

		public override void GetItemDimensions(out int width, out int height){
			throw new System.NotImplementedException();
		}

		public override void AddRecipes(){
			CreateRecipe(1)
				.AddIngredient(ItemID.BoneWelder, 1)
				.AddIngredient(ItemID.GlassKiln, 1)
				.AddIngredient(ItemID.HoneyDispenser, 1)
				.AddIngredient(ItemID.IceMachine, 1)
				.AddIngredient(ItemID.LivingLoom, 1)
				.AddIngredient(ItemID.SkyMill, 1)
				.AddIngredient(ItemID.Solidifier, 1)
				.Register();
		}
	}
}
