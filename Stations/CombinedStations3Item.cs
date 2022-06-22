using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	//Overwrite the base class logic
	[Autoload(true)]
	public class CombinedStations3Item : CombinedStationsItem<CombinedStations3Tile>
	{
		public override string ItemName => "Combined Stations (Tier 3)";

		public override string ItemDescription => "Combines the functionality of several crafting stations";

		public override Dictionary<GameCulture, string> ItemNameLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "高级组合制作站" }
				};

		public override Dictionary<GameCulture, string> ItemDescriptionLocalized => new Dictionary<GameCulture, string>
				{
					{ GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "结合了部分高级制作站的功能" }
				};

		public override int Rarity => ItemRarityID.Yellow;

		public override void SafeSetDefaults() {
			Item.value = BasePriceFromItems((ModContent.ItemType<CombinedStations2Item>(), 1),
				(ItemID.ImbuingStation, 1),
				(ItemID.MythrilAnvil, 1),
				(ItemID.AdamantiteForge, 1),
				(ItemID.Bookcase, 1),
				(ItemID.CrystalBall, 1),
				(ItemID.BlendOMatic, 1),
				(ItemID.MeatGrinder, 1));
		}

		public override void GetItemDimensions(out int width, out int height) {
			width = 30;
			height = 30;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<CombinedStations2Item>())
				.AddIngredient(ItemID.ImbuingStation)
				.AddRecipeGroup("MagicStorage:AnyHmAnvil")
				.AddRecipeGroup("MagicStorage:AnyHmFurnace")
				.AddRecipeGroup("MagicStorage:AnyBookcase")
				.AddIngredient(ItemID.CrystalBall)
				.AddIngredient(ItemID.BlendOMatic)
				.AddIngredient(ItemID.MeatGrinder)
				.Register();
		}
	}
}
