using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class CombinedStations4Item : CombinedStationsItem<CombinedStations4Tile>
	{
		public override int Rarity => ItemRarityID.Purple;

		public override void SafeSetDefaults()
		{
			Item.value = BasePriceFromItems(
				(ModContent.ItemType<CombinedStations3Item>(), 1),
				(ModContent.ItemType<CombinedFurnitureStations2Item>(), 1),
				(ItemID.Autohammer, 1),
				(ItemID.LunarCraftingStation, 1),
				(ItemID.LavaBucket, 30),
				(ItemID.HoneyBucket, 30));
		}

		public override void GetItemDimensions(out int width, out int height)
		{
			width = 30;
			height = 30;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<CombinedStations3Item>()
				.AddIngredient<CombinedFurnitureStations2Item>()
				.AddIngredient(ItemID.Autohammer)
				.AddIngredient(ItemID.LunarCraftingStation)
				.AddIngredient(ItemID.LavaBucket, 10)
				.AddIngredient(ItemID.HoneyBucket, 10)
				.Register();
		}
	}
}
