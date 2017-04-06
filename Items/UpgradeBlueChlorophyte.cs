using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeBlueChlorophyte : ModItem
	{
		public override void SetDefaults()
		{
			item.name = "Blue Chlorophyte Storage Upgrade";
			item.toolTip = "Upgrades Storage Unit to 240 capacity";
			item.toolTip2 = "Right-click a Hallowed Storage Unit to use";
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 7;
			item.value = Item.sellPrice(0, 1, 0, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.ShroomiteBar, 5);
			recipe.AddIngredient(ItemID.SpectreBar, 5);
			recipe.AddIngredient(ItemID.BeetleHusk, 2);
			recipe.AddIngredient(ItemID.Emerald);
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}