using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	public class DemonAltar : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Demon Altar");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "恶魔祭坛");
			Tooltip.SetDefault("A placeable Demon Altar. Cannot be mined to get hardmode ores");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "可放置的恶魔祭坛复制品，试图挖掉的话什么都不会发生");
		}

		public override void SetDefaults() {
			Item.width = 48;
			Item.height = 34;
			Item.rare = ItemRarityID.Green;
			Item.createTile = ModContent.TileType<EvilAltarTile>();
			Item.placeStyle = 0;
			Item.maxStack = 99;
			Item.value = Item.sellPrice(silver: 15);
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.useTurn = true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.DemoniteBar, 10)
				.AddIngredient(ItemID.ShadowScale, 15)
				.AddTile(TileID.DemonAltar)
				.Register();
		}
	}
}
