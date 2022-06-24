using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class CombinedFurnitureStations2Item : CombinedStationsItem<CombinedFurnitureStations2Tile>
	{
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
