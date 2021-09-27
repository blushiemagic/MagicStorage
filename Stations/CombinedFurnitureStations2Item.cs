using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Overwrite the base class logic
	[Autoload(true)]
	public class CombinedFurnitureStations2Item : CombinedStationsItem<CombinedFurnitureStations2Tile>
	{
		public override string ItemName => "Combined Furniture Stations (Tier 2)";

		public override string ItemDescription => "Combines the functionality of several crafting stations for furniture";

		public override int Rarity => ItemRarityID.Pink;

		public override void SafeSetDefaults()
		{
			Item.value = BasePriceFromItems((ModContent.ItemType<CombinedFurnitureStations1Item>(), 1),
				(ItemID.LesionStation, 1),
				(ItemID.FleshCloningVaat, 1),
				(ItemID.SteampunkBoiler, 1),
				(ItemID.LihzahrdFurnace, 1));
		}

		public override void GetItemDimensions(out int width, out int height)
		{
			width = 30;
			height = 30;
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
