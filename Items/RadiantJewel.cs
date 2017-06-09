using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class RadiantJewel : ModItem
	{
		public override void SetStaticDefaults()
		{
			Tooltip.SetDefault("'Shines with a dazzling light'");
			DisplayName.AddTranslation(GameCulture.Russian, "Сияющая драгоценность");
			Tooltip.AddTranslation(GameCulture.Russian, "'Блестит ослепительным светом'");
		}

		public override void SetDefaults()
		{
			item.width = 14;
			item.height = 14;
			item.maxStack = 99;
			item.rare = 11;
			item.value = Item.sellPrice(0, 10, 0, 0);
		}

		public override Color? GetAlpha(Color lightColor)
		{
			return Color.White;
		}

		public override void PostUpdate()
		{
			Lighting.AddLight(item.position, 1f, 1f, 1f);
		}
	}
}
