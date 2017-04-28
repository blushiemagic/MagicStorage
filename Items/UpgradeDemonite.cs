using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeDemonite : ModItem
	{
		public override void SetDefaults()
		{
			item.name = "Demonite Storage Upgrade";
			item.toolTip = "Upgrades Storage Unit to 80 capacity";
			item.toolTip2 = "Right-click a Storage Unit to use";
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 1;
			item.value = Item.sellPrice(0, 0, 32, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.DemoniteBar, 10);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Amethyst);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyAmethyst");
			}
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}