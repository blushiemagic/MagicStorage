using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class CreativeStorageUnit : ModItem
	{
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
			Item.createTile = ModContent.TileType<Components.CreativeStorageUnit>();
		}
	}
}
