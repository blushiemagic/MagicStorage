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
			DisplayName.AddTranslation(GameCulture.Russian, "Сияющая Драгоценность");
			DisplayName.AddTranslation(GameCulture.Polish, "Promieniejący klejnot");
			DisplayName.AddTranslation(GameCulture.French, "Bijou Rayonnant");
			DisplayName.AddTranslation(GameCulture.Spanish, "Joya Radiante");

			Tooltip.SetDefault("'Shines with a dazzling light'");
			Tooltip.AddTranslation(GameCulture.Russian, "'Блестит ослепительным светом'");
			Tooltip.AddTranslation(GameCulture.Polish, "'Świeci oślepiającym światłem'");
			Tooltip.AddTranslation(GameCulture.French, "'Il brille avec une lumière aveuglante'");
			Tooltip.AddTranslation(GameCulture.Spanish, "'Brilla con una luz deslumbrante'");
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
