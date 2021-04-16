using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorageExtra.Items
{
	public class CreativeStorageUnit : ModItem
	{

		public override void SetStaticDefaults() {
			DisplayName.AddTranslation(GameCulture.Russian, "Креативная Ячейка Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Kreatywna Jednostka Magazynująca");
			DisplayName.AddTranslation(GameCulture.French, "Unité de Stockage Créatif");
			DisplayName.AddTranslation(GameCulture.Spanish, "Unidad de Almacenamiento Creativa");
			DisplayName.AddTranslation(GameCulture.Chinese, "创造储存单元");
		}

		public override void SetDefaults() {
			item.width = 26;
			item.height = 26;
			item.maxStack = 99;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.consumable = true;
			item.rare = ItemRarityID.White;
			item.createTile = ModContent.TileType<Components.CreativeStorageUnit>();
		}
	}
}
