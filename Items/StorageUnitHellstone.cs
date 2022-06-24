using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitHellstone : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 10;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 99;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.sellPrice(silver: 50);
			Item.createTile = ModContent.TileType<Components.StorageUnit>();
			Item.placeStyle = 3;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<StorageUnitDemonite>();
			recipe.AddIngredient<UpgradeHellstone>();
			recipe.Register();

			recipe = CreateRecipe();
			recipe.AddIngredient<StorageUnitCrimtane>();
			recipe.AddIngredient<UpgradeHellstone>();
			recipe.Register();
		}
	}
}
