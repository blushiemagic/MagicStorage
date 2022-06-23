using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class CombinedFurnitureStations1Item : CombinedStationsItem<CombinedFurnitureStations1Tile>
	{
		public override string ItemName => "Combined Furniture Stations (Tier 1)";

		public override string ItemDescription => "Combines the functionality of several crafting stations for furniture";

		public override Dictionary<GameCulture, string> ItemNameLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "基础组合家具制作站" }
				};

		public override Dictionary<GameCulture, string> ItemDescriptionLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "结合了部分基础家具制作站的功能" }
				};

		public override int Rarity => ItemRarityID.Green;

		public override void SafeSetDefaults()
		{
			Item.value = BasePriceFromItems((ItemID.BoneWelder, 1),
				(ItemID.GlassKiln, 1),
				(ItemID.HoneyDispenser, 1),
				(ItemID.IceMachine, 1),
				(ItemID.LivingLoom, 1),
				(ItemID.SkyMill, 1),
				(ItemID.Solidifier, 1));
		}

		public override void GetItemDimensions(out int width, out int height)
		{
			width = 30;
			height = 30;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BoneWelder)
				.AddIngredient(ItemID.GlassKiln)
				.AddIngredient(ItemID.HoneyDispenser)
				.AddIngredient(ItemID.IceMachine)
				.AddIngredient(ItemID.LivingLoom)
				.AddIngredient(ItemID.SkyMill)
				.AddIngredient(ItemID.Solidifier)
				.Register();
		}
	}
}
