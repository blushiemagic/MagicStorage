using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeLuminite : ModItem
	{
		public override void SetDefaults()
		{
			item.name = "Luminite Storage Upgrade";
			item.toolTip = "Upgrades Storage Unit to 320 capacity";
			item.toolTip2 = "Right-click a Blue Chlorophyte Storage Unit to use";
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 10;
			item.value = Item.sellPrice(0, 1, 50, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.LunarBar, 10);
			recipe.AddIngredient(ItemID.FragmentSolar, 5);
			recipe.AddIngredient(ItemID.FragmentVortex, 5);
			recipe.AddIngredient(ItemID.FragmentNebula, 5);
			recipe.AddIngredient(ItemID.FragmentStardust, 5);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Ruby);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyRuby");
			}
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}