using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Overwrite the base class logic
	[Autoload(true)]
	public class CombinedStations1Item : CombinedStationsItem<CombinedStations1Tile>
	{
		public override string ItemName => "Combined Stations (Tier 1)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override Dictionary<GameCulture, string> ItemNameLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "基础组合制作站" }
				};

		public override Dictionary<GameCulture, string> ItemDescriptionLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "结合了部分基础制作站的功能" }
				};

		public override int Rarity => ItemRarityID.Green;

		public override void SafeSetDefaults()
		{
			Item.value = BasePriceFromItems((ItemID.WorkBench, 1),
				(ItemID.Furnace, 1),
				(ItemID.IronAnvil, 1),
				(ItemID.Bottle, 1),
				(ItemID.MetalSink, 1),
				(ItemID.Sawmill, 1),
				(ItemID.Loom, 1),
				(ItemID.WoodenTable, 1));
		}

		public override void GetItemDimensions(out int width, out int height)
		{
			width = 30;
			height = 30;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddRecipeGroup("MagicStorage:AnyWorkBench")
				.AddIngredient(ItemID.Furnace)
				.AddRecipeGroup("MagicStorage:AnyPreHmAnvil")
				.AddRecipeGroup("MagicStorage:AnyBottle")
				.AddRecipeGroup("MagicStorage:AnySink")
				.AddIngredient(ItemID.Sawmill)
				.AddIngredient(ItemID.Loom)
				.AddRecipeGroup("MagicStorage:AnyTable")
				.Register();
		}
	}
}
