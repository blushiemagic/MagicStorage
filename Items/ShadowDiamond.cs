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
			DisplayName.AddTranslation(GameCulture.Russian, "Теневой алмаз");
			DisplayName.AddTranslation(GameCulture.Polish, "Mroczny Diament");
			DisplayName.AddTranslation(GameCulture.French, "Diamant sombre");
			DisplayName.AddTranslation(GameCulture.Spanish, "Diamante sombreado");

			Tooltip.SetDefault("Traces of light still linger inside");
			Tooltip.AddTranslation(GameCulture.Russian, "Следы света все еще мелькают внутри");
			Tooltip.AddTranslation(GameCulture.Polish, "Ślady światła wciąż pozostają w środku");
			Tooltip.AddTranslation(GameCulture.French, "Des traces de lumière s'attarde encore à l'intérieur");
			Tooltip.AddTranslation(GameCulture.Spanish, "Sigue habiendo huellas de luz en el interior");
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
