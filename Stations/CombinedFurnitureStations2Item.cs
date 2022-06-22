using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Overwrite the base class logic
	[Autoload(true)]
	public class CombinedFurnitureStations2Item : CombinedStationsItem<CombinedFurnitureStations2Tile>
	{
		public override string ItemName => "Combined Furniture Stations (Tier 2)";

		public override string ItemDescription => "Combines the functionality of several crafting stations for furniture";

		public override Dictionary<GameCulture, string> ItemNameLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "进阶组合家具制作站" }
				};

		public override Dictionary<GameCulture, string> ItemDescriptionLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "结合了部分进阶家具制作站的功能" }
				};

		public override int Rarity => ItemRarityID.Pink;

		public override void SafeSetDefaults() {
			Item.value = BasePriceFromItems((ModContent.ItemType<CombinedFurnitureStations1Item>(), 1),
				(ItemID.LesionStation, 1),
				(ItemID.FleshCloningVaat, 1),
				(ItemID.SteampunkBoiler, 1),
				(ItemID.LihzahrdFurnace, 1));
		}

		public override void GetItemDimensions(out int width, out int height) {
			width = 30;
			height = 30;
		}

		public override void AddRecipes() {
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
