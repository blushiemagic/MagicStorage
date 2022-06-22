using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Overwrite the base class logic
	[Autoload(true)]
	public class CombinedStations2Item : CombinedStationsItem<CombinedStations2Tile>
	{
		public override string ItemName => "Combined Stations (Tier 2)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override Dictionary<GameCulture, string> ItemNameLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "进阶组合制作站" }
				};

		public override Dictionary<GameCulture, string> ItemDescriptionLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "结合了部分进阶制作站的功能" }
				};

		public override int Rarity => ItemRarityID.Pink;

		public override void SafeSetDefaults()
		{
			Item.value = BasePriceFromItems((ModContent.ItemType<CombinedFurnitureStations1Item>(), 1),
				(ItemID.AlchemyTable, 1),
				(ItemID.CookingPot, 1),
				(ItemID.TinkerersWorkshop, 1),
				(ItemID.DyeVat, 1),
				(ItemID.HeavyWorkBench, 1),
				(ItemID.TeaKettle, 1));
		}

		public override void GetItemDimensions(out int width, out int height)
		{
			width = 30;
			height = 30;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<CombinedStations1Item>())
				.AddIngredient(ItemID.AlchemyTable)
				.AddRecipeGroup("MagicStorage:AnyCookingPot")
				.AddIngredient(ItemID.TinkerersWorkshop)
				.AddIngredient(ItemID.DyeVat)
				.AddIngredient(ItemID.HeavyWorkBench)
				.AddIngredient(ItemID.Keg)
				.AddIngredient(ItemID.TeaKettle)
				.Register();
		}
	}
}
