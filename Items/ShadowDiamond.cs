using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class ShadowDiamond : ModItem
	{
		public override void SetStaticDefaults()
		{
			Tooltip.SetDefault("Traces of light still linger inside");
			DisplayName.AddTranslation(GameCulture.Russian, "Теневой алмаз");
			Tooltip.AddTranslation(GameCulture.Russian, "Следы света все еще мелькают внутри");
		}
		

		public override void SetDefaults()
		{
			item.width = 16;
			item.height = 16;
			item.maxStack = 99;
			item.rare = 1;
			item.value = Item.sellPrice(0, 1, 0, 0);
		}

		public override Color? GetAlpha(Color lightColor)
		{
			return Color.White;
		}
	}
}
