using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class LocatorDisk : Locator
	{
		public override void SetDefaults()
		{
			item.name = "Locator Drive";
			item.toolTip = "Right-click Storage Heart to store location";
			item.toolTip2 = "Does not get destroyed upon use";
			item.width = 28;
			item.height = 28;
			item.maxStack = 1;
			item.rare = 10;
			item.value = Item.sellPrice(0, 5, 0, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.MartianConduitPlating, 25);
			recipe.AddIngredient(ItemID.LunarBar, 5);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}