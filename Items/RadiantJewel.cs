using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class RadiantJewel : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 5;
		}

		public override void SetDefaults()
		{
			Item.width = 14;
			Item.height = 14;
			Item.maxStack = 99;
			Item.rare = ItemRarityID.Purple;
			Item.value = Item.sellPrice(gold: 10);
		}

		public override Color? GetAlpha(Color lightColor) => Color.White;

		public override void PostUpdate()
		{
			Lighting.AddLight(Item.position, 1f, 1f, 1f);
		}
	}
}
