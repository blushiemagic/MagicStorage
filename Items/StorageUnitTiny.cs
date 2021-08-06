using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageUnitTiny : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Tiny Storage Unit");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Малая Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Mała jednostka magazynująca");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Unité de Stockage Miniscule");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Unidad de Almacenamiento Minúsculo");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储单元(小)");
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
			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(0, 0, 6);
			Item.createTile = ModContent.TileType<Components.StorageUnit>();
			Item.placeStyle = 8;
		}
	}
}
